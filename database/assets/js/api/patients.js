/**
 * Patients API
 */
import { get } from './client.js';

export async function getPatients(params = {}) { return get('/api/patients', params); }
export async function getPatientById(id) { return get(`/api/patients/${id}`); }
export async function getPatientStudies(id) { return get(`/api/patients/${id}/studies`); }
export async function getPatientRiskProfile(id) { return get(`/api/patients/${id}/risk-profile`); }

export default { getPatients, getPatientById, getPatientStudies, getPatientRiskProfile };
