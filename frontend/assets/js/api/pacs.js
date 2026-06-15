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

export default { getPacsStudies, ingestPacsStudy };
