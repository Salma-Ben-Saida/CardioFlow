/**
 * PACS API
 */
import { get, post } from './client.js';

export async function getPacsStudies() {
    return get('/api/pacs/studies');
}

export async function ingestPacsStudy(accessionNumber) {
    return post(`/api/pacs/ingest/${encodeURIComponent(accessionNumber)}`);
}

// Used by the Vite viewer to resolve series + instance UIDs before loading images
export async function getDicomInstances(studyInstanceUid) {
    return get(`/api/dicom/study/${encodeURIComponent(studyInstanceUid)}/instances`);
}

export default { getPacsStudies, ingestPacsStudy, getDicomInstances };
