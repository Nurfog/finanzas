import axios from 'axios';

const api = axios.create({
    baseURL: '/api',
    headers: {
        'Content-Type': 'application/json',
    },
});

export const AnalyticsService = {
    getRevenue: (startDate, endDate) => api.get('/analytics/revenue', { params: { startDate, endDate } }),
    getRevenueByLocation: (startDate, endDate) => api.get('/analytics/revenue/by-location', { params: { startDate, endDate } }),
    predictRevenue: (locationId, monthsAhead) => api.get('/analytics/revenue/predictions', { params: { locationId, monthsAhead } }),
    getCustomerSegments: (startDate, endDate) => api.get('/analytics/customers/segments', { params: { startDate, endDate } }),
    getRoomUsage: (startDate, endDate) => api.get('/analytics/rooms/usage', { params: { startDate, endDate } }),
    getRoomUtilization: (locationId, startDate, endDate) => api.get('/analytics/rooms/utilization', { params: { locationId, startDate, endDate } }),
    getRoomPatterns: (locationId) => api.get('/analytics/rooms/patterns', { params: { locationId } }),
    getUnderutilizedRooms: (threshold) => api.get('/analytics/rooms/underutilized', { params: { threshold } }),
    getRoomOptimization: () => api.get('/analytics/rooms/optimization'),
    getStudentPerformance: (startDate, endDate) => api.get('/analytics/students/performance', { params: { startDate, endDate } }),
};

export const ReportsService = {
    getAll: () => api.get('/reports'),
    getById: (id) => api.get(`/reports/${id}`),
    generateRevenue: (startDate, endDate) => api.post('/reports/generate/revenue', null, { params: { startDate, endDate } }),
    generateStudent: (startDate, endDate) => api.post('/reports/generate/students', null, { params: { startDate, endDate } }),
    generateRoom: (startDate, endDate) => api.post('/reports/generate/rooms', null, { params: { startDate, endDate } }),
    generateCustomer: (startDate, endDate) => api.post('/reports/generate/customers', null, { params: { startDate, endDate } }),
    downloadExcel: (startDate, endDate) => api.post('/reports/generate/revenue', null, {
        params: { startDate, endDate },
        responseType: 'blob' // Important for binary data
    }),
};

export default api;
