/**
 * PACS API
 */
import { get, post } from './client.js';

export async function getPacsStudies() {
    return get('/api/pacs/studies');
}
export async function getPacsOverview() {
    return get('/api/pacs/overview');
}
export async function ingestPacsStudy(accessionNumber, patientOverride) {
    return post(`/api/pacs/ingest/${encodeURIComponent(accessionNumber)}`, patientOverride);
}

// Used by the Vite viewer to resolve series + instance UIDs before loading images
export async function getDicomInstances(studyInstanceUid) {
    return get(`/api/dicom/study/${encodeURIComponent(studyInstanceUid)}/instances`);
}

export default { getPacsStudies, getPacsOverview, ingestPacsStudy, getDicomInstances };
