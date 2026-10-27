import {
  init as initCore,
  RenderingEngine,
  Enums,
  metaData,
  cache
} from '@cornerstonejs/core';

import {
  init as initTools,
  ToolGroupManager,
  ZoomTool,
  PanTool,
  StackScrollTool,
  addTool,
  Enums as ToolEnums,
  utilities
} from '@cornerstonejs/tools';

import cornerstoneDICOMImageLoader from '@cornerstonejs/dicom-image-loader';

// ── CONSTANTS ─────────────────────────────────────────────────────────────────
const API_BASE = `${location.protocol}//${location.hostname}:5152`;
let authToken = new URLSearchParams(location.hash.slice(1)).get('token') || '';
if (authToken) {
  history.replaceState(null, '', location.pathname + location.search); // drop token from the address bar
}
const authHeaders = () => {
  if (!authToken) {
    return {};
  }
  return { Authorization: `Bearer ${authToken}` };
};
const DICOMWEB_BASE = `${API_BASE}/api/dicom/web`;

// ── Metadata maps ─────────────────────────────────────────────────────────────
const customMetadataCache   = new Map(); // instanceUID → metadata obj
const imageIdToInstanceUID  = new Map(); // full wadors imageId → instanceUID

// ── Metadata provider ─────────────────────────────────────────────────────────
// Priority 10000: must beat cornerstoneDICOMImageLoader's own built-in provider
// which runs at priority 0 and caches a stale/empty result before our maps
// are populated. The decode worker queries metadata BEFORE setStack resolves,
// so we must be the highest-priority provider in the chain.
metaData.addProvider((type, imageId) => {
  const instanceUID = imageIdToInstanceUID.get(imageId);
  if (!instanceUID) return undefined;
  const metadata = customMetadataCache.get(instanceUID);
  if (!metadata) return undefined;

  if (type === 'imagePixelModule') {
    return {
      samplesPerPixel:           parseInt(metadata['00280002']?.Value?.[0] ?? 1),
      photometricInterpretation: metadata['00280004']?.Value?.[0] ?? 'MONOCHROME2',
      rows:                      parseInt(metadata['00280010']?.Value?.[0] ?? 512),
      columns:                   parseInt(metadata['00280011']?.Value?.[0] ?? 512),
      bitsAllocated:             parseInt(metadata['00280100']?.Value?.[0] ?? 16),
      bitsStored:                parseInt(metadata['00280101']?.Value?.[0] ?? 12),
      highBit:                   parseInt(metadata['00280102']?.Value?.[0] ?? 11),
      pixelRepresentation:       parseInt(metadata['00280103']?.Value?.[0] ?? 0),
    };
  }

  if (type === 'imagePlaneModule') {
    // Defaults here matter: XA projection images very often omit orientation/
    // position tags entirely (unlike CT/MR), since 2D fluoro doesn't carry the
    // same 3D patient-space geometry. Without these fallbacks, Cornerstone's
    // actor-construction step silently fails to build a renderable actor.
    return {
      pixelSpacing:         metadata['00280030']?.Value ?? [1, 1],
      rowCosines:           metadata['00200037']?.Value?.slice(0, 3) ?? [1, 0, 0],
      columnCosines:        metadata['00200037']?.Value?.slice(3, 6) ?? [0, 1, 0],
      imagePositionPatient: metadata['00200032']?.Value ?? [0, 0, 0],
    };
  }

  if (type === 'transferSyntax') {
    const ts = metadata['00020010']?.Value?.[0];
    if (ts) return { transferSyntaxUID: ts };
  }

  if (type === 'generalSeriesModule') {
    // Without this, Cornerstone's buildMetadata() destructures `modality` off
    // an undefined result and throws — silently, inside a Promise.allSettled
    // in loadImages(), so no actor ever gets created and the canvas stays
    // black with zero visible errors.
    return {
      modality: metadata['00080060']?.Value?.[0] ?? 'XA',
    };
  }

  return undefined;
}, 10000); // ← 10000, not 100. Must outrank the loader's built-in provider.

let isCinePlaying = false;
let currentFPS = 10;

// ─────────────────────────────────────────────────────────────────────────────
async function main() {
  // ── Verify cross-origin isolation ─────────────────────────────────────────
  // Cornerstone3D uses OffscreenCanvas.transferControlToOffscreen() for its
  // WebGL pipeline. If crossOriginIsolated is false, the GPU→canvas blit is
  // silently blocked by the browser and the viewport stays black.
  // Fix: add COOP/COEP headers in vite.config.js (see companion file).
  if (!window.crossOriginIsolated) {
    console.error(
      '[CS3D] crossOriginIsolated is FALSE.\n' +
      'The WebGL OffscreenCanvas cannot blit to the DOM canvas.\n' +
      'Add to vite.config.js server.headers:\n' +
      '  Cross-Origin-Opener-Policy: same-origin\n' +
      '  Cross-Origin-Embedder-Policy: require-corp\n' +
      'Then hard-refresh (Ctrl+Shift+R).'
    );
    showError(
      'Missing COOP/COEP headers — viewport will be black.<br>' +
      'Add Cross-Origin-Opener-Policy + Cross-Origin-Embedder-Policy to vite.config.js'
    );
    // Do NOT return — continue init so the rest of the pipeline can still be
    // tested, but the canvas blit will fail until headers are added.
  } else {
    console.log('[CS3D] crossOriginIsolated ✓');
  }

  await initCore();

  if (!authToken) {
    showError('Open the viewer from CardioFlow');
    return;
  }

  await cornerstoneDICOMImageLoader.init({
    maxWebWorkers: Math.max(navigator.hardwareConcurrency - 1, 1),
    startWebWorkersOnDemand: true,
    taskConfiguration: {
      decodeTask: {
        initializeCodecsOnStartup: true,
        strict: false,
      },
    },
    beforeSend: async () => ({
      Authorization: authHeaders().Authorization || '',
    }),
  });

  await initTools();
  addTool(ZoomTool);
  addTool(PanTool);
  addTool(StackScrollTool);

  const params   = new URLSearchParams(location.search);
  const studyUID = params.get('studyUID');
  if (!studyUID) { showError('No studyUID provided.'); return; }

  if (!authToken) {
    showError('Open the viewer from CardioFlow');
    return;
  }

  const data = await fetch(
    `${API_BASE}/api/dicom/study/${encodeURIComponent(studyUID)}/instances`,
    { headers: authHeaders() }
  ).then(r => {
    if (r.status === 401) {
      throw Object.assign(new Error('SESSION_EXPIRED'), { status: 401 });
    }
    if (r.status === 403) {
      throw Object.assign(new Error('FORBIDDEN'), { status: 403 });
    }
    if (!r.ok) throw new Error(`HTTP ${r.status}`);
    return r.json();
  }).catch(err => {
    if (err?.status === 401) {
      showError('Session expired, reopen the viewer from CardioFlow');
      throw err;
    }
    if (err?.status === 403) {
      showError('Your role is not allowed to view images');
      throw err;
    }
    showError(err?.message || 'Unable to load study data');
    throw err;
  });

  if (!data.series?.length) { showError('No series found for this study.'); return; }

  const viewportEl = document.getElementById('viewport');
  const engine     = new RenderingEngine('viewer');

  // Exposed for console debugging — safe to keep, harmless in production.
  window.__csEngine   = engine;
  window.__csCache    = cache;
  window.__csMetaData = metaData;

  engine.enableElement({
    viewportId: 'STACK',
    type:       Enums.ViewportType.STACK,
    element:    viewportEl,
  });

  const toolGroup = ToolGroupManager.createToolGroup('DEFAULT');
  toolGroup.addViewport('STACK', 'viewer');
  toolGroup.addTool(ZoomTool.toolName);
  toolGroup.addTool(PanTool.toolName);
  toolGroup.addTool(StackScrollTool.toolName);

  toolGroup.setToolActive(PanTool.toolName, {
    bindings: [{ mouseButton: ToolEnums.MouseBindings.Primary }],
  });
  toolGroup.setToolActive(ZoomTool.toolName, {
    bindings: [{ mouseButton: ToolEnums.MouseBindings.Secondary }],
  });
  toolGroup.setToolActive(StackScrollTool.toolName, {
    bindings: [{ mouseButton: ToolEnums.MouseBindings.Wheel }],
  });

  viewportEl.addEventListener(Enums.Events.STACK_NEW_IMAGE, (evt) => {
    const infoElem = document.getElementById('instance-info');
    if (!infoElem) return;
    // evt.detail shape (CS3D 5.x): { image, imageId, imageIdIndex, viewportId, renderingEngineId }
    // There is no `imageIds` array on the event itself — ask the viewport directly.
    const { imageIdIndex } = evt.detail;
    const totalImages = engine.getViewport('STACK').getImageIds().length;
    infoElem.textContent = `${imageIdIndex + 1} / ${totalImages}`;
  });

  document.getElementById('btnPlay')?.addEventListener('click', () => {
    const stackViewport = engine.getViewport('STACK');
    const btn = document.getElementById('btnPlay');
    const ids = stackViewport?.getImageIds() ?? [];
    if (!ids.length) { console.warn('No images loaded yet'); return; }

    if (isCinePlaying) {
      utilities.cine.stopClip(viewportEl);
      isCinePlaying = false;
      btn.textContent = '▶ Play';
    } else {
      utilities.cine.playClip(viewportEl, { framesPerSecond: currentFPS });
      isCinePlaying = true;
      btn.textContent = '⏸ Pause';
    }
  });

  // Cine speed control — lets the doctor adjust playback FPS on the fly.
  // Expects an <input id="fpsInput"> (range or number) in the HTML, plus an
  // optional <span id="fpsValue"> to echo the current value.
  const fpsInput = document.getElementById('fpsInput');
  const fpsValue = document.getElementById('fpsValue');
  if (fpsInput) {
    fpsInput.value = currentFPS;
    if (fpsValue) fpsValue.textContent = `${currentFPS} fps`;

    fpsInput.addEventListener('input', () => {
      const newFPS = parseInt(fpsInput.value, 10);
      if (isNaN(newFPS) || newFPS <= 0) return;

      currentFPS = newFPS;
      if (fpsValue) fpsValue.textContent = `${currentFPS} fps`;

      // utilities.cine has no "set speed while running" call — the reliable
      // way to apply a new rate immediately is to restart the clip at it.
      if (isCinePlaying) {
        utilities.cine.stopClip(viewportEl);
        utilities.cine.playClip(viewportEl, { framesPerSecond: currentFPS });
      }
    });
  }

  // Suppress the browser's native right-click context menu so the
  // drag-to-zoom gesture (bound to the Zoom tool's secondary mouse button)
  // isn't interrupted by a popup — needed on both Windows and Mac browsers.
  viewportEl.addEventListener('contextmenu', (e) => e.preventDefault());

  // ── Zoom controls — buttons, keyboard shortcuts, and ctrl+wheel/trackpad
  // pinch, all driving the same zoomBy() helper so they stay in sync.
  const btnZoomIn     = document.getElementById('btnZoomIn');
  const btnZoomOut    = document.getElementById('btnZoomOut');
  const btnZoomReset  = document.getElementById('btnZoomReset');
  const ZOOM_STEP     = 1.15; // ~15% per click / keypress / wheel notch

  btnZoomIn?.addEventListener('click', () => zoomBy(engine.getViewport('STACK'), ZOOM_STEP));
  btnZoomOut?.addEventListener('click', () => zoomBy(engine.getViewport('STACK'), 1 / ZOOM_STEP));
  btnZoomReset?.addEventListener('click', () => {
    const stackViewport = engine.getViewport('STACK');
    if (!stackViewport) return;
    stackViewport.resetCamera();
    stackViewport.render();
    updateZoomReadout(stackViewport);
  });

  // Ctrl on Windows/Linux, Cmd on Mac — checking both means no platform
  // sniffing is needed for either the shortcuts or the wheel/pinch handler.
  let zoomModifierHeld = false;

  window.addEventListener('keydown', (e) => {
    if (!(e.ctrlKey || e.metaKey)) return;

    if (!zoomModifierHeld) {
      zoomModifierHeld = true;
      // Park StackScrollTool while the zoom modifier is held so the same
      // wheel event doesn't simultaneously change slices and zoom below.
      toolGroup.setToolPassive(StackScrollTool.toolName);
    }

    if (e.key === '+' || e.key === '=') {
      e.preventDefault(); // otherwise the browser's own page-zoom fires too
      zoomBy(engine.getViewport('STACK'), ZOOM_STEP);
    } else if (e.key === '-' || e.key === '_') {
      e.preventDefault();
      zoomBy(engine.getViewport('STACK'), 1 / ZOOM_STEP);
    } else if (e.key === '0') {
      e.preventDefault();
      btnZoomReset?.click();
    }
  });

  window.addEventListener('keyup', (e) => {
    if (!(e.ctrlKey || e.metaKey) && zoomModifierHeld) {
      zoomModifierHeld = false;
      toolGroup.setToolActive(StackScrollTool.toolName, {
        bindings: [{ mouseButton: ToolEnums.MouseBindings.Wheel }],
      });
    }
  });

  // Ctrl+wheel is how browsers represent BOTH Windows "Ctrl + scroll-wheel"
  // zoom AND Mac trackpad pinch-to-zoom (pinch gestures arrive as wheel
  // events with ctrlKey=true) — one handler covers both platforms' native
  // zoom gesture without detecting which OS the doctor is on.
  viewportEl.addEventListener('wheel', (e) => {
    if (!e.ctrlKey) return; // plain scroll stays bound to StackScrollTool (slice)
    e.preventDefault();
    const factor = e.deltaY < 0 ? ZOOM_STEP : 1 / ZOOM_STEP;
    zoomBy(engine.getViewport('STACK'), factor);
  }, { passive: false });

  // Flatten series→instances into one row per instance. Each series' two
  // instances (8-bit / 16-bit) get their own independent sidebar entry,
  // sorted so the 16-bit copy is listed first, in original series order.
  const infoElem = document.getElementById('instance-info');
  if (infoElem) infoElem.textContent = 'Fetching series metadata…';

  const displayItems = await buildDisplayItems(data.series);

  renderSeriesSidebar(displayItems, engine);
  await loadInstance(displayItems[0], engine);
}

// ─────────────────────────────────────────────────────────────────────────────
// Fetches DICOMweb JSON metadata (no pixel data — cheap) for a single
// instance and pulls out the handful of tags we need: bit depth (to sort/
// label) and frame count (to build the frame imageIds later).
async function fetchInstanceMeta(series, inst) {
  const studyUID    = series.StudyInstanceUID ?? series.studyUID;
  const seriesUID   = series.SeriesInstanceUID ?? series.seriesUID;
  const instanceUID = inst.SOPInstanceUID ?? inst.instanceUID;

  const metadataUrl =
    `${DICOMWEB_BASE}/studies/${studyUID}/series/${seriesUID}` +
    `/instances/${instanceUID}/metadata`;

  const json = await fetch(metadataUrl, { headers: authHeaders() }).then(r => {
    if (r.status === 401) throw Object.assign(new Error('SESSION_EXPIRED'), { status: 401 });
    if (r.status === 403) throw Object.assign(new Error('FORBIDDEN'), { status: 403 });
    if (!r.ok) throw new Error(`Metadata fetch failed: ${r.status}`);
    return r.json();
  });

  const metadataObj   = Array.isArray(json) ? json[0] : json;
  const bitsAllocated = parseInt(metadataObj['00280100']?.Value?.[0] ?? 0);

  const frameCount = (() => {
    const tag = metadataObj['00280008'];
    if (!tag?.Value) return 1;
    const v = Array.isArray(tag.Value) ? tag.Value[0] : tag.Value;
    return parseInt(v) || 1;
  })();

  return { series, inst, instanceUID, metadataObj, bitsAllocated, frameCount };
}

// ─────────────────────────────────────────────────────────────────────────────
// Builds the flat list the sidebar renders from. For each series, fetches
// both instances' metadata in parallel, sorts them descending by
// bitsAllocated (16-bit first, 8-bit underneath), and flattens everything
// into one array — series order preserved via Promise.all's index ordering,
// not completion order.
async function buildDisplayItems(seriesList) {
  const perSeriesMetas = await Promise.all(
    seriesList.map(async (series, seriesIndex) => {
      const metas = await Promise.all(
        series.instances.map(inst => fetchInstanceMeta(series, inst))
      );

      metas.sort((a, b) => b.bitsAllocated - a.bitsAllocated); // 16-bit, then 8-bit

      metas.forEach(meta => {
        console.log(
          `Series ${seriesIndex + 1} / instance ${meta.instanceUID}: ` +
          `frames=${meta.frameCount}, bitsAllocated=${meta.bitsAllocated}`
        );
      });

      return metas.map(meta => ({ ...meta, seriesIndex }));
    })
  );

  return perSeriesMetas.flat();
}

// ─────────────────────────────────────────────────────────────────────────────
// Loads exactly one instance. No more picking between 8-bit/16-bit here —
// that decision already happened in buildDisplayItems; each sidebar row maps
// 1:1 to a single, already-resolved instance.
async function loadInstance(item, engine) {
  const { series, instanceUID, metadataObj, frameCount, bitsAllocated } = item;
  const studyUID  = series.StudyInstanceUID ?? series.studyUID;
  const seriesUID = series.SeriesInstanceUID ?? series.seriesUID;
  const infoElem  = document.getElementById('instance-info');
  const imageIds  = [];

  if (infoElem) infoElem.textContent = `Loading ${bitsAllocated}-bit instance…`;

  customMetadataCache.set(instanceUID, metadataObj);

  for (let f = 0; f < frameCount; f++) {
    const wadorsId =
      `wadors:${DICOMWEB_BASE}/studies/${studyUID}/series/${seriesUID}` +
      `/instances/${instanceUID}/frames/${f + 1}`;
    imageIds.push(wadorsId);
    imageIdToInstanceUID.set(wadorsId, instanceUID);
  }

  console.log(`imageIds WADO-RS: ${imageIds.length}`);

  const stackViewport = engine.getViewport('STACK');

  console.time('setStack');
  try {
    await stackViewport.setStack(imageIds, 0);
    console.log('setStack ✓');
  } catch (err) {
    console.error('setStack failed:', err);
    showError(`Failed to load DICOM: ${err.message}`);
    return;
  }
  console.timeEnd('setStack');

  // VOI: prefer WindowCenter/WindowWidth from metadata
  const wc = parseFloat(metadataObj['00281050']?.Value?.[0] ?? NaN);
  const ww = parseFloat(metadataObj['00281051']?.Value?.[0] ?? NaN);

  if (!isNaN(wc) && !isNaN(ww) && ww > 0) {
    stackViewport.setProperties({ voiRange: { lower: wc - ww / 2, upper: wc + ww / 2 } });
    console.log(`VOI from metadata WC:${wc} WW:${ww}`);
  } else {
    const bitsStored = parseInt(metadataObj['00280101']?.Value?.[0] ?? 8);
    const defaultWidth  = Math.pow(2, bitsStored);
    const defaultCenter = defaultWidth / 2;
    stackViewport.setProperties({
      voiRange: { lower: defaultCenter - defaultWidth / 2, upper: defaultCenter + defaultWidth / 2 }
    });
    console.log(`VOI fallback from bitsStored=${bitsStored}: range 0–${defaultWidth}`);
  }

  stackViewport.resetCamera();
  stackViewport.render();
  updateZoomReadout(stackViewport);
  if (infoElem) infoElem.textContent = `1 / ${imageIds.length}`;
}

// ─────────────────────────────────────────────────────────────────────────────
// Applies a relative zoom change. `factor` > 1 zooms in, < 1 zooms out.
// viewport.getZoom()/setZoom() track magnification relative to the
// fit-to-window state set by the last resetCamera() call (1.0 == 100%) —
// the same baseline the Zoom tool's own mouse-drag uses internally, so
// button/keyboard/wheel zoom all stay in sync with drag-zoom.
function zoomBy(viewport, factor) {
  if (!viewport) return;
  const MIN_ZOOM = 0.1;
  const MAX_ZOOM = 20;
  const newZoom = Math.min(Math.max(viewport.getZoom() * factor, MIN_ZOOM), MAX_ZOOM);
  viewport.setZoom(newZoom);
  viewport.render();
  updateZoomReadout(viewport);
}

function updateZoomReadout(viewport) {
  const zoomLabel = document.getElementById('zoomValue');
  if (!zoomLabel || !viewport) return;
  zoomLabel.textContent = `${Math.round(viewport.getZoom() * 100)}%`;
}

// ─────────────────────────────────────────────────────────────────────────────
function renderSeriesSidebar(displayItems, engine) {
  const sidebar    = document.getElementById('series-sidebar');
  const viewportEl = document.getElementById('viewport');
  if (!sidebar) return;
  sidebar.innerHTML = '';

  displayItems.forEach((item, i) => {
    const thumb = document.createElement('div');
    thumb.className = 'series-thumb' + (i === 0 ? ' active' : '');
    thumb.innerHTML = `
      <img data-src="${API_BASE}/api/dicom/preview/${item.inst.orthancId}"
           crossorigin="anonymous" alt="Series ${item.seriesIndex + 1} (${item.bitsAllocated}-bit)"
           onerror="this.style.display='none'">
      <span>Series ${item.seriesIndex + 1} (${item.bitsAllocated}-bit)</span>
      <span class="count">${item.frameCount} img</span>
    `;
    const _img = thumb.querySelector('img');
  fetch(_img.dataset.src, { headers: authHeaders() })
  .then(r => r.ok ? r.blob() : Promise.reject())
  .then(b => { _img.src = URL.createObjectURL(b); })
  .catch(() => { _img.style.display = 'none'; });
    thumb.addEventListener('click', async () => {
      if (isCinePlaying) { utilities.cine.stopClip(viewportEl); isCinePlaying = false; }
      document.getElementById('btnPlay').textContent = '▶ Play';
      document.querySelectorAll('.series-thumb').forEach(t => t.classList.remove('active'));
      thumb.classList.add('active');
      await loadInstance(item, engine);
    });
    sidebar.appendChild(thumb);
  });
}

function showError(msg) {
  const v = document.getElementById('viewport');
  if (v) v.innerHTML = `<p style="color:#f87171;padding:2rem">${msg}</p>`;
}

main().catch((err) => {
  console.error(err);
  showError(err?.status === 401
    ? 'Session expired, reopen the viewer from CardioFlow'
    : err?.status === 403
      ? 'Your role is not allowed to view images'
      : 'Unable to load the viewer. Please reopen it from CardioFlow.');
});