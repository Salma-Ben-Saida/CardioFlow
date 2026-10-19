/**
 * Cath studies planning API
 */
import { get, post, put } from './client.js';

export const getStudies    = () => get('/api/cathstudies');
export const scheduleStudy = (id, scheduledAtIso) => put(`/api/cathstudies/${id}/schedule`, { scheduledAt: scheduledAtIso });
export const cancelStudy   = (id) => post(`/api/cathstudies/${id}/cancel`);

export default { getStudies, scheduleStudy, cancelStudy };