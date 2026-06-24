import { defineConfig } from 'vite';
import commonjs from 'vite-plugin-commonjs';

export default defineConfig({
  plugins: [
    commonjs({
      filter(id) {
        if (id.includes('@cornerstonejs/codec-')) {
          return true;
        }
      },
    }),
  ],
  server: {
    port: 5501,
    headers: {
      'Cross-Origin-Opener-Policy': 'same-origin',
      'Cross-Origin-Embedder-Policy': 'require-corp',
    },
    // AJOUT D'UN PROXY INVERSE : Vite va lui-même faire la passerelle CORS
    proxy: {
      '/orthanc-wadors': {
        target: 'http://localhost:8042',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/orthanc-wadors/, ''),
      }
    }
  },
  build: {
    outDir: '../frontend/pages/studies/viewer-dist',
    commonjsOptions: {
      include: [/node_modules/],
    },
  },
  optimizeDeps: {
    exclude: ['@cornerstonejs/dicom-image-loader'],
    include: ['@kitware/vtk.js', 'gl-matrix', 'globalthis', 'dicom-parser']
  },
  assetsInclude: ['**/*.wasm'],
});
