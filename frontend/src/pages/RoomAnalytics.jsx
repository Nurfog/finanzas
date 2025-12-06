import React, { useState, useEffect } from 'react';
import { AnalyticsService } from '../services/api';
import DateRangeSelector from '../components/DateRangeSelector';
import {
    BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
    LineChart, Line, ReferenceLine
} from 'recharts';

const RoomAnalytics = () => {
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [utilizationData, setUtilizationData] = useState({ byLocation: [], overallCapacityUtilization: 0, overallTimeUtilization: 0 });
    const [patternsData, setPatternsData] = useState({ byDayOfWeek: [] });
    const [underutilizedData, setUnderutilizedData] = useState({ underutilizedRooms: [], totalRoomsUnderutilized: 0, totalWastedHours: 0 });
    const [optimizationData, setOptimizationData] = useState({ suggestions: [], totalSuggestions: 0 });
    const [selectedLocation, setSelectedLocation] = useState('');

    // Initialize with last 30 days
    const [startDate, setStartDate] = useState(() => {
        const d = new Date();
        d.setDate(d.getDate() - 30);
        return d;
    });
    const [endDate, setEndDate] = useState(() => new Date());

    useEffect(() => {
        fetchData();
    }, [selectedLocation, startDate, endDate]);

    const fetchData = async () => {
        setLoading(true);
        setError(null);
        try {
            const [utilization, patterns, underutilized, optimization] = await Promise.all([
                AnalyticsService.getRoomUtilization(selectedLocation || null, startDate.toISOString(), endDate.toISOString()),
                AnalyticsService.getRoomPatterns(selectedLocation || null),
                AnalyticsService.getUnderutilizedRooms(0.5),
                AnalyticsService.getRoomOptimization()
            ]);

            setUtilizationData(utilization.data || { byLocation: [], overallCapacityUtilization: 0, overallTimeUtilization: 0 });
            setPatternsData(patterns.data || { byDayOfWeek: [] });
            setUnderutilizedData(underutilized.data || { underutilizedRooms: [], totalRoomsUnderutilized: 0, totalWastedHours: 0 });
            setOptimizationData(optimization.data || { suggestions: [], totalSuggestions: 0 });
        } catch (error) {
            console.error('Error fetching room analytics:', error);
            setError('No se pudieron cargar los datos de análisis. Por favor intente nuevamente.');
        } finally {
            setLoading(false);
        }
    };

    if (loading) {
        return (
            <div className="flex justify-center items-center h-screen">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="flex justify-center items-center h-screen">
                <div className="text-center p-8 bg-red-50 rounded-xl border border-red-200">
                    <p className="text-red-600 font-medium">{error}</p>
                    <button
                        onClick={fetchData}
                        className="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
                    >
                        Reintentar
                    </button>
                </div>
            </div>
        );
    }

    const dayTranslation = {
        'Monday': 'Lunes',
        'Tuesday': 'Martes',
        'Wednesday': 'Miércoles',
        'Thursday': 'Jueves',
        'Friday': 'Viernes',
        'Saturday': 'Sábado',
        'Sunday': 'Domingo'
    };

    const translatedPatterns = patternsData?.byDayOfWeek?.map(d => ({
        ...d,
        dayOfWeek: dayTranslation[d.dayOfWeek] || d.dayOfWeek
    })) || [];

    return (
        <div className="p-6 bg-gray-50 min-h-screen">
            <div className="mb-8 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
                <div>
                    <h1 className="text-3xl font-bold text-gray-800">Análisis de Uso de Salas</h1>
                    <p className="text-gray-600 mt-2">Optimización de espacios y recursos (8:30 - 21:15)</p>
                </div>
                <DateRangeSelector
                    startDate={startDate}
                    endDate={endDate}
                    onRangeChange={(start, end) => {
                        setStartDate(start);
                        setEndDate(end);
                    }}
                />
            </div>

            {/* KPI Cards */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
                {/* Ocupación Promedio */}
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100 relative group">
                    <h3 className="text-gray-500 text-sm font-medium">Ocupación Promedio (Capacidad)</h3>
                    <p className="text-3xl font-bold text-blue-600 mt-2">
                        {(utilizationData?.overallCapacityUtilization * 100).toFixed(1)}%
                    </p>
                    <p className="text-xs text-gray-400 mt-1">Promedio Global</p>

                    {/* Tooltip-like breakdown */}
                    <div className="mt-4 pt-4 border-t border-gray-100 text-sm space-y-1">
                        {utilizationData?.byLocation?.map(loc => (
                            <div key={loc.locationId} className="flex justify-between">
                                <span className="text-gray-600">{loc.locationName}</span>
                                <span className="font-medium text-blue-600">{(loc.capacityUtilization * 100).toFixed(0)}%</span>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Uso de Tiempo */}
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <h3 className="text-gray-500 text-sm font-medium">Uso de Tiempo (Frecuencia)</h3>
                    <p className="text-3xl font-bold text-indigo-600 mt-2">
                        {(utilizationData?.overallTimeUtilization * 100).toFixed(1)}%
                    </p>
                    <p className="text-xs text-gray-400 mt-1">Promedio Global</p>
                    <div className="mt-4 pt-4 border-t border-gray-100 text-sm space-y-1">
                        {utilizationData?.byLocation?.map(loc => (
                            <div key={loc.locationId} className="flex justify-between">
                                <span className="text-gray-600">{loc.locationName}</span>
                                <span className="font-medium text-indigo-600">{(loc.timeUtilization * 100).toFixed(0)}%</span>
                            </div>
                        ))}
                    </div>
                </div>

                {/* Horas Desperdiciadas */}
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <h3 className="text-gray-500 text-sm font-medium">Horas Desperdiciadas</h3>
                    <p className="text-3xl font-bold text-orange-500 mt-2">
                        {Math.round(underutilizedData?.totalWastedHours || 0).toLocaleString()}
                    </p>
                    <p className="text-xs text-gray-400 mt-1">Horas disponibles sin uso</p>
                </div>

                {/* Oportunidades */}
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <h3 className="text-gray-500 text-sm font-medium">Oportunidades de Optimización</h3>
                    <p className="text-3xl font-bold text-green-600 mt-2">
                        {optimizationData?.totalSuggestions || 0}
                    </p>
                    <p className="text-xs text-gray-400 mt-1">Sugerencias de mejora</p>
                </div>
            </div>

            {/* Charts Section */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8 mb-8">
                {/* Utilization by Location */}
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <h3 className="text-lg font-semibold mb-2">Utilización por Sede</h3>
                    <p className="text-sm text-gray-500 mb-6">Comparativa de Uso de Tiempo (frecuencia) vs Ocupación de Capacidad (llenado).</p>
                    <div className="h-80">
                        <ResponsiveContainer width="100%" height="100%">
                            <BarChart data={utilizationData?.byLocation || []} margin={{ top: 20, right: 30, left: 20, bottom: 20 }}>
                                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                                <XAxis dataKey="locationName" label={{ value: 'Sede', position: 'insideBottom', offset: -10 }} />
                                <YAxis tickFormatter={(value) => `${(value * 100).toFixed(0)}%`} label={{ value: 'Porcentaje (%)', angle: -90, position: 'insideLeft' }} />
                                <Tooltip formatter={(value, name) => [
                                    `${(value * 100).toFixed(1)}%`,
                                    name === 'timeUtilization' ? 'Uso de Tiempo' : 'Ocupación Capacidad'
                                ]} />
                                <Legend verticalAlign="top" height={36} />
                                <ReferenceLine y={utilizationData?.overallTimeUtilization} label="Prom. Tiempo" stroke="red" strokeDasharray="3 3" />
                                <Bar dataKey="timeUtilization" name="Uso de Tiempo" fill="#3B82F6" radius={[4, 4, 0, 0]} />
                                <Bar dataKey="capacityUtilization" name="Ocupación Capacidad" fill="#10B981" radius={[4, 4, 0, 0]} />
                            </BarChart>
                        </ResponsiveContainer>
                    </div>
                    <p className="text-xs text-gray-400 mt-4 text-center">
                        * Uso de Tiempo: % de horas ocupadas respecto al total disponible (8:30-21:15). <br />
                        * Ocupación Capacidad: % de asientos ocupados cuando la sala está en uso.
                    </p>
                </div>

                {/* Usage Patterns */}
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <h3 className="text-lg font-semibold mb-2">Patrones por Día de Semana</h3>
                    <p className="text-sm text-gray-500 mb-6">Cantidad de sesiones y ocupación promedio por día.</p>
                    <div className="h-80">
                        <ResponsiveContainer width="100%" height="100%">
                            <BarChart data={translatedPatterns} margin={{ top: 20, right: 30, left: 20, bottom: 20 }}>
                                <CartesianGrid strokeDasharray="3 3" vertical={false} />
                                <XAxis dataKey="dayOfWeek" label={{ value: 'Día', position: 'insideBottom', offset: -10 }} />
                                <YAxis yAxisId="left" orientation="left" stroke="#3B82F6" label={{ value: 'Sesiones', angle: -90, position: 'insideLeft' }} />
                                <YAxis yAxisId="right" orientation="right" stroke="#10B981" tickFormatter={(value) => `${(value * 100).toFixed(0)}%`} label={{ value: 'Ocupación (%)', angle: 90, position: 'insideRight' }} />
                                <Tooltip />
                                <Legend verticalAlign="top" height={36} />
                                <Bar yAxisId="left" dataKey="sessionCount" name="Sesiones" fill="#3B82F6" radius={[4, 4, 0, 0]} />
                                <Line yAxisId="right" type="monotone" dataKey="averageUtilization" name="Ocupación" stroke="#10B981" strokeWidth={2} />
                            </BarChart>
                        </ResponsiveContainer>
                    </div>
                    <p className="text-xs text-gray-400 mt-4 text-center">
                        Muestra la carga de trabajo por día de la semana para identificar días pico.
                    </p>
                </div>
            </div>

            {/* Optimization Suggestions */}
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 mb-8">
                <div className="lg:col-span-2 bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <h3 className="text-lg font-semibold mb-2">Recomendaciones de Optimización</h3>
                    <p className="text-sm text-gray-500 mb-6">Sugerencias basadas en datos para mejorar la eficiencia.</p>
                    <div className="space-y-4">
                        {optimizationData?.suggestions?.slice(0, 5).map((suggestion, index) => (
                            <div key={index} className="flex items-start p-4 bg-gray-50 rounded-lg border border-gray-100">
                                <div className={`p-2 rounded-full mr-4 ${suggestion.type === 'Downsize' ? 'bg-orange-100 text-orange-600' :
                                    suggestion.type === 'Expand' ? 'bg-blue-100 text-blue-600' :
                                        'bg-green-100 text-green-600'
                                    }`}>
                                    {suggestion.type === 'Downsize' && <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M19 14l-7 7m0 0l-7-7m7 7V3"></path></svg>}
                                    {suggestion.type === 'Expand' && <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M5 10l7-7m0 0l7 7m-7-7v18"></path></svg>}
                                    {suggestion.type === 'Consolidate' && <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4"></path></svg>}
                                </div>
                                <div>
                                    <h4 className="font-semibold text-gray-800">{suggestion.roomName} - {suggestion.locationName}</h4>
                                    <p className="text-gray-600 text-sm mt-1">{suggestion.suggestion}</p>
                                    <p className="text-sm font-medium text-blue-600 mt-2">
                                        {suggestion.potentialSavings || suggestion.potentialImpact}
                                    </p>
                                </div>
                            </div>
                        ))}
                        {(!optimizationData?.suggestions || optimizationData.suggestions.length === 0) && (
                            <p className="text-gray-500 text-center py-4">No hay recomendaciones disponibles en este momento.</p>
                        )}
                    </div>
                </div>

                {/* Top Underutilized Rooms */}
                <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
                    <h3 className="text-lg font-semibold mb-2">Salas Menos Utilizadas</h3>
                    <p className="text-sm text-gray-500 mb-6">Salas con menor tiempo de uso.</p>
                    <div className="overflow-x-auto">
                        <table className="min-w-full">
                            <thead>
                                <tr className="text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                                    <th className="pb-3">Sede</th>
                                    <th className="pb-3">Sala</th>
                                    <th className="pb-3">Uso Tiempo</th>
                                    <th className="pb-3">Ocupación</th>
                                </tr>
                            </thead>
                            <tbody className="divide-y divide-gray-100">
                                {underutilizedData?.underutilizedRooms?.slice(0, 8).map((room, index) => (
                                    <tr key={index}>
                                        <td className="py-3 text-sm text-gray-500">{room.locationName}</td>
                                        <td className="py-3 text-sm font-medium text-gray-900">{room.roomName}</td>
                                        <td className="py-3 text-sm text-red-500 font-medium">
                                            {(room.timeUtilization * 100).toFixed(1)}%
                                        </td>
                                        <td className="py-3 text-sm text-gray-500">
                                            {(room.capacityUtilization * 100).toFixed(1)}%
                                        </td>
                                    </tr>
                                ))}
                                {(!underutilizedData?.underutilizedRooms || underutilizedData.underutilizedRooms.length === 0) && (
                                    <tr>
                                        <td colSpan="4" className="py-4 text-center text-gray-500 text-sm">No hay salas subutilizadas detectadas.</td>
                                    </tr>
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    );
};

export default RoomAnalytics;
