import { useState, useEffect } from 'react';
import { AnalyticsService } from '../services/api';

export default function Rooms() {
    const [data, setData] = useState(null);

    useEffect(() => {
        AnalyticsService.getRoomUsage().then(res => setData(res.data));
    }, []);

    return (
        <div className="space-y-6">
            <h2 className="text-2xl font-bold">Análisis de Uso de Salas</h2>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {data?.byRoom?.map((room, index) => (
                    <div key={index} className="card">
                        <div className="flex justify-between items-start mb-4">
                            <div>
                                <h3 className="text-lg font-semibold">{room.roomName}</h3>
                                <p className="text-sm text-slate-400">{room.locationName}</p>
                            </div>
                            <div className={`px-3 py-1 rounded-full text-sm font-bold ${room.averageUtilization > 80 ? 'bg-red-500/20 text-red-400' :
                                    room.averageUtilization > 50 ? 'bg-green-500/20 text-green-400' :
                                        'bg-yellow-500/20 text-yellow-400'
                                }`}>
                                {Math.round(room.averageUtilization)}% Uso
                            </div>
                        </div>

                        <div className="space-y-3">
                            <div>
                                <div className="flex justify-between text-sm mb-1">
                                    <span className="text-slate-400">Tasa de Ocupación</span>
                                    <span>{Math.round(room.averageUtilization)}%</span>
                                </div>
                                <div className="w-full bg-slate-700 rounded-full h-2">
                                    <div
                                        className={`h-2 rounded-full ${room.averageUtilization > 80 ? 'bg-red-500' :
                                                room.averageUtilization > 50 ? 'bg-green-500' :
                                                    'bg-yellow-500'
                                            }`}
                                        style={{ width: `${room.averageUtilization}%` }}
                                    ></div>
                                </div>
                            </div>

                            <div className="grid grid-cols-2 gap-4 mt-4">
                                <div className="bg-slate-800 p-3 rounded-lg text-center">
                                    <p className="text-xs text-slate-400">Sesiones</p>
                                    <p className="text-xl font-bold">{room.totalSessions}</p>
                                </div>
                                <div className="bg-slate-800 p-3 rounded-lg text-center">
                                    <p className="text-xs text-slate-400">Asistentes</p>
                                    <p className="text-xl font-bold">{room.totalAttendees}</p>
                                </div>
                            </div>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}
