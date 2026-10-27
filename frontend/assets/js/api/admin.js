/**
 * Admin API: PACS overview, archive / restore / purge
 */
import { get, post, put, del } from './client.js';

// PACS (Orthanc) overview with ingestion state, and manual ingestion of everything new
export const getPacsOverview = () => get('/api/admin/pacs/studies');
export const ingestAllPacs   = () => post('/api/pacs/ingest/all');

// Archive
export const getArchive      = () => get('/api/admin/archive');

export const archiveStudy    = (id, reason) => post(`/api/admin/studies/${id}/archive`, { reason });
export const restoreStudy    = (id) => post(`/api/admin/studies/${id}/restore`);
export const purgeStudy      = (id) => del(`/api/admin/studies/${id}/purge`);

export const archivePatient  = (id, reason) => post(`/api/admin/patients/${id}/archive`, { reason });
export const restorePatient  = (id) => post(`/api/admin/patients/${id}/restore`);
export const purgePatient    = (id) => del(`/api/admin/patients/${id}/purge`);

// Ingestion period 
export const getIngestionSettings = () => get('/api/admin/ingestion-settings');
export const setIngestionSettings = (intervalMinutes) => put('/api/admin/ingestion-settings', { intervalMinutes });

export default {
    getPacsOverview, ingestAllPacs, getArchive,
    archiveStudy, restoreStudy, purgeStudy,
    archivePatient, restorePatient, purgePatient,
    getIngestionSettings, setIngestionSettings
};
