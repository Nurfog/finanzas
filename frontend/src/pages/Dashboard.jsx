import { useState, useEffect } from 'react';
import { AnalyticsService } from '../services/api';
import { Line, Doughnut, Bar } from 'react-chartjs-2';

function KPICard({ title, value, trend, icon, color }) {
    return (
        <div className="card">
            <div className="flex justify-between items-start">
                <div>
                    <p className="text-slate-400 text-sm font-medium">{title}</p>
                    <h3 className="text-2xl font-bold text-white mt-1">{value}</h3>
                </div>
                <div className={`p-2 rounded-lg bg-${color}-500/20 text-${color}-400`}>
                    {icon}
                </div>
            </div>
            <div className="mt-4 flex items-center text-sm">
                <span className={trend >= 0 ? "text-success" : "text-danger"}>
                    {trend >= 0 ? "↑" : "↓"} {Math.abs(trend)}%
                </span>
                <span className="text-slate-500 ml-2">vs mes anterior</span>
            </div>
        </div>
    );
}

export default function Dashboard() {
    const [loading, setLoading] = useState(true);
    const [revenueData, setRevenueData] = useState(null);
    const [roomData, setRoomData] = useState(null);
    const [studentData, setStudentData] = useState(null);

    useEffect(() => {
        const fetchData = async () => {
            try {
                const [revenue, rooms, students] = await Promise.all([
                    AnalyticsService.getRevenue(),
                    AnalyticsService.getRoomUsage(),
                    AnalyticsService.getStudentPerformance()
                ]);

                setRevenueData(revenue.data);
                setRoomData(rooms.data);
                setStudentData(students.data);
            } catch (error) {
                console.error("Error fetching dashboard data", error);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, []);

    if (loading) {
        return <div className="flex justify-center items-center h-full text-accent">Cargando datos...</div>;
    }

    // Chart Data Preparation
    const revenueChartData = {
        labels: revenueData?.byMonth?.map(m => `${m.month}/${m.year}`) || [],
        datasets: [
            {
                label: 'Ingresos Mensuales',
                data: revenueData?.byMonth?.map(m => m.revenue) || [],
                borderColor: '#3b82f6',
                backgroundColor: 'rgba(59, 130, 246, 0.1)',
                fill: true,
                tension: 0.4
            }
        ]
    };

    const roomChartData = {
        labels: roomData?.byDayOfWeek?.map(d => d.dayOfWeek) || [],
        datasets: [
            {
                label: 'Utilización Promedio (%)',
                data: roomData?.byDayOfWeek?.map(d => d.averageUtilization) || [],
                backgroundColor: '#10b981',
                borderRadius: 4,
            }
        ]
    };

    return (
        <div className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <KPICard
                    title="Ingresos Totales"
                    value={`$${revenueData?.totalRevenue?.toLocaleString()}`}
                    trend={12.5}
                    icon="💰"
                    color="blue"
                />
                <KPICard
                    title="Transacciones"
                    value={revenueData?.transactionCount}
                    trend={5.2}
                    icon="💳"
                    color="purple"
                />
                <KPICard
                    title="Uso de Salas"
                    value={`${Math.round(roomData?.overallUtilization || 0)}%`}
                    trend={-2.1}
                    icon="🏢"
                    color="green"
                />
                <KPICard
                    title="Estudiantes Activos"
                    value={studentData?.totalStudents}
                    trend={8.4}
                    icon="🎓"
                    color="orange"
                />
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                <div className="card lg:col-span-2">
                    <h3 className="text-lg font-semibold mb-4">Tendencia de Ingresos</h3>
                    <div className="h-64">
                        <Line data={revenueChartData} options={{ maintainAspectRatio: false, plugins: { legend: { display: false } } }} />
                    </div>
                </div>

                <div className="card">
                    <h3 className="text-lg font-semibold mb-4">Ocupación Semanal</h3>
                    <div className="h-64">
                        <Bar data={roomChartData} options={{ maintainAspectRatio: false, plugins: { legend: { display: false } } }} />
                    </div>
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <div className="card">
                    <h3 className="text-lg font-semibold mb-4">Rendimiento Estudiantil</h3>
                    <div className="overflow-x-auto">
                        <table className="w-full text-sm text-left">
                            <thead className="text-xs text-slate-400 uppercase bg-slate-800/50">
                                <tr>
                                    <th className="px-4 py-3">Nivel</th>
                                    <th className="px-4 py-3">Estudiantes</th>
                                    <th className="px-4 py-3">Promedio</th>
                                </tr>
                            </thead>
                            <tbody>
                                {studentData?.byPerformance?.map((item, index) => (
                                    <tr key={index} className="border-b border-slate-700">
                                        <td className="px-4 py-3 font-medium">{item.performanceLevel}</td>
                                        <td className="px-4 py-3">{item.studentCount}</td>
                                        <td className="px-4 py-3">{Math.round(item.averageScore)}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                </div>

                <div className="card">
                    <h3 className="text-lg font-semibold mb-4">Métodos de Pago</h3>
                    <div className="flex items-center justify-center h-64">
                        <Doughnut
                            data={{
                                labels: revenueData?.byPaymentMethod?.map(p => p.paymentMethod) || [],
                                datasets: [{
                                    data: revenueData?.byPaymentMethod?.map(p => p.revenue) || [],
                                    backgroundColor: ['#3b82f6', '#10b981', '#f59e0b', '#ef4444'],
                                    borderWidth: 0
                                }]
                            }}
                            options={{ maintainAspectRatio: false }}
                        />
                    </div>
                </div>
            </div>
        </div>
    );
}
