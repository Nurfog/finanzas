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
    getCustomerSegments: () => api.get('/analytics/customers/segments'),
    getRoomUsage: () => api.get('/analytics/rooms/usage'),
    getStudentPerformance: () => api.get('/analytics/students/performance'),
};

export const ReportsService = {
    getAll: () => api.get('/reports'),
    getById: (id) => api.get(`/reports/${id}`),
    generateRevenue: (startDate, endDate) => api.post('/reports/generate/revenue', null, { params: { startDate, endDate } }),
    generateStudent: (startDate, endDate) => api.post('/reports/generate/students', null, { params: { startDate, endDate } }),
    generateRoom: (startDate, endDate) => api.post('/reports/generate/rooms', null, { params: { startDate, endDate } }),
    generateCustomer: (startDate, endDate) => api.post('/reports/generate/customers', null, { params: { startDate, endDate } }),
};

export default api;
