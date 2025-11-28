import { useState, useEffect } from 'react';
import { AnalyticsService } from '../services/api';

export default function Customers() {
    const [segments, setSegments] = useState(null);

    useEffect(() => {
        AnalyticsService.getCustomerSegments().then(res => setSegments(res.data));
    }, []);

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-bold">Segmentación de Clientes (IA)</h2>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {segments?.summary?.map((segment, index) => (
                    <div key={index} className="card border-t-4 border-t-accent">
                        <h3 className="text-lg font-semibold mb-2">Segmento {segment.segment}</h3>
                        <div className="space-y-2">
                            <div className="flex justify-between">
                                <span className="text-slate-400">Clientes:</span>
                                <span className="font-bold">{segment.customerCount}</span>
                            </div>
                            <div className="flex justify-between">
                                <span className="text-slate-400">Gasto Promedio:</span>
                                <span className="font-bold">${Math.round(segment.averageSpent).toLocaleString()}</span>
                            </div>
                            <div className="flex justify-between">
                                <span className="text-slate-400">Total Ingresos:</span>
                                <span className="font-bold text-success">${segment.totalRevenue.toLocaleString()}</span>
                            </div>
                        </div>
                    </div>
                ))}
            </div>

            <div className="card">
                <h3 className="text-lg font-semibold mb-4">Detalle de Clientes</h3>
                <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                        <thead className="text-xs text-slate-400 uppercase bg-slate-800/50">
                            <tr>
                                <th className="px-4 py-3">Cliente</th>
                                <th className="px-4 py-3">Segmento</th>
                                <th className="px-4 py-3">Transacciones</th>
                                <th className="px-4 py-3">Total Gastado</th>
                            </tr>
                        </thead>
                        <tbody>
                            {segments?.segments?.map((customer, index) => (
                                <tr key={index} className="border-b border-slate-700 hover:bg-slate-800/50">
                                    <td className="px-4 py-3 font-medium">{customer.customerName}</td>
                                    <td className="px-4 py-3">
                                        <span className="px-2 py-1 rounded bg-slate-700 text-xs">
                                            Segmento {customer.segment}
                                        </span>
                                    </td>
                                    <td className="px-4 py-3">{customer.transactionCount}</td>
                                    <td className="px-4 py-3">${customer.totalSpent.toLocaleString()}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    );
}
