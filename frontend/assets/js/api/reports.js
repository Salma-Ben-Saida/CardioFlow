/**
 * Reports API
 */
import { get } from './client.js';

export async function getReports(params = {}) {
    return get('/api/reports', params);
}
export const getPatientReports = (patientId) => get(`/api/reports/patient/${patientId}`);