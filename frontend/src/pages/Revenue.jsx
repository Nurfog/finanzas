import { useState, useEffect } from 'react';
import { AnalyticsService } from '../services/api';
import { Line } from 'react-chartjs-2';

export default function Revenue() {
    const [data, setData] = useState(null);
    const [predictions, setPredictions] = useState(null);
    const [locationId, setLocationId] = useState(1);

    useEffect(() => {
        AnalyticsService.getRevenueByLocation().then(res => setData(res.data));
        AnalyticsService.predictRevenue(locationId, 6).then(res => setPredictions(res.data));
    }, [locationId]);

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-bold">Análisis de Ingresos</h2>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <div className="card">
                    <h3 className="text-lg font-semibold mb-4">Ingresos por Sede</h3>
                    <div className="overflow-x-auto">
                        <table className="w-full text-sm text-left">
                            <thead className="text-xs text-slate-400 uppercase bg-slate-800/50">
                                <tr>
                                    <th className="px-4 py-3">Sede</th>
                                    <th className="px-4 py-3">Transacciones</th>
                                    <th className="px-4 py-3">Ingresos</th>
                                </tr>
                            </thead>
                            <tbody>
                                {data?.map((item, index) => (
                                    <tr key={index} className="border-b border-slate-700">
                                        <td className="px-4 py-3 font-medium">{item.locationName}</td>
                                        <td className="px-4 py-3">{item.transactionCount}</td>
                                        <td className="px-4 py-3">${item.revenue.toLocaleString()}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>

                <div className="card">
                    <div className="flex justify-between items-center mb-4">
                        <h3 className="text-lg font-semibold">Predicción de Ingresos (IA)</h3>
                        <select
                            value={locationId}
                            onChange={(e) => setLocationId(Number(e.target.value))}
                            className="bg-slate-800 border border-slate-600 rounded px-2 py-1 text-sm"
                        >
                            <option value={1}>Sede Central</option>
                            <option value={2}>Sede Norte</option>
                            <option value={3}>Sede Sur</option>
                        </select>
                    </div>

                    {predictions?.predictions ? (
                        <div className="h-64">
                            <Line
                                data={{
                                    labels: predictions.predictions.map(p => `${p.month}/${p.year}`),
                                    datasets: [{
                                        label: 'Ingresos Predichos',
                                        data: predictions.predictions.map(p => p.predictedRevenue),
                                        borderColor: '#8b5cf6',
                                        borderDash: [5, 5],
                                        fill: false
                                    }]
                                }}
                                options={{ maintainAspectRatio: false }}
                            />
                        </div>
                    ) : (
                        <div className="flex items-center justify-center h-64 text-slate-500">
                            Cargando predicciones...
                        </div>
                    )}
                    <p className="text-xs text-slate-400 mt-4">
                        * Predicciones generadas por modelo de regresión ML.NET basado en histórico transaccional.
                    </p>
                </div>
            </div>
        </div>
    );
}
