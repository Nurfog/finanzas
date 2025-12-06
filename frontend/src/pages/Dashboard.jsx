import { useState, useEffect } from 'react';
import { AnalyticsService } from '../services/api';
import { Line, Doughnut, Bar } from 'react-chartjs-2';
import {
    Chart as ChartJS,
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    BarElement,
    Title,
    Tooltip,
    Legend,
    ArcElement,
    Filler
} from 'chart.js';
import html2canvas from 'html2canvas';
import jsPDF from 'jspdf';

ChartJS.register(
    CategoryScale,
    LinearScale,
    PointElement,
    LineElement,
    BarElement,
    Title,
    Tooltip,
    Legend,
    ArcElement,
    Filler
);

function KPICard({ title, value, trend, icon, color, gradient }) {
    return (
        <div className="card relative overflow-hidden group hover:border-slate-600 transition-all duration-300">
            <div className={`absolute top-0 right-0 w-32 h-32 bg-${color}-500/10 rounded-full blur-3xl -mr-16 -mt-16 transition-all group-hover:bg-${color}-500/20`}></div>

            <div className="relative z-10">
                <div className="flex justify-between items-start mb-4">
                    <div className={`p-3 rounded-xl bg-gradient-to-br ${gradient} shadow-lg shadow-${color}-500/20`}>
                        <span className="text-2xl">{icon}</span>
                    </div>
                    {trend !== 0 && (
                        <div className={`flex items-center px-2 py-1 rounded-lg text-xs font-medium ${trend >= 0 ? 'bg-emerald-500/10 text-emerald-400' : 'bg-rose-500/10 text-rose-400'}`}>
                            {trend >= 0 ? "↑" : "↓"} {Math.abs(trend)}%
                        </div>
                    )}
                </div>

                <div>
                    <p className="text-slate-400 text-sm font-medium tracking-wide uppercase">{title}</p>
                    <h3 className="text-3xl font-bold text-white mt-1 tracking-tight">{value}</h3>
                </div>
            </div>
        </div>
    );
}

function InsightBadge({ type, children }) {
    const colors = {
        positive: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
        warning: 'bg-amber-500/10 text-amber-400 border-amber-500/20',
        info: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
        neutral: 'bg-slate-500/10 text-slate-400 border-slate-500/20'
    };
    const icons = {
        positive: '🚀',
        warning: '⚠️',
        info: '💡',
        neutral: '📊'
    };
    return (
        <div className={`px-4 py-3 rounded-lg border flex items-start gap-3 ${colors[type] || colors.info}`}>
            <span className="text-xl mt-0.5">{icons[type] || icons.info}</span>
            <span className="text-sm font-medium leading-relaxed">{children}</span>
        </div>
    );
}

import DateRangeSelector from '../components/DateRangeSelector';

export default function Dashboard() {
    const [loading, setLoading] = useState(true);
    const [revenueData, setRevenueData] = useState(null);
    const [roomData, setRoomData] = useState(null);
    const [studentData, setStudentData] = useState(null);
    const [startDate, setStartDate] = useState(() => {
        const d = new Date();
        d.setMonth(d.getMonth() - 6); // Default to 6 months to ensure data visibility
        return d;
    });
    const [endDate, setEndDate] = useState(() => new Date());

    useEffect(() => {
        const fetchData = async () => {
            console.log("Fetching dashboard data for:", startDate.toISOString(), endDate.toISOString());
            try {
                const [revenue, rooms, students] = await Promise.all([
                    AnalyticsService.getRevenue(startDate.toISOString(), endDate.toISOString()),
                    AnalyticsService.getRoomUsage(startDate.toISOString(), endDate.toISOString()),
                    AnalyticsService.getStudentPerformance(startDate.toISOString(), endDate.toISOString())
                ]);

                console.log("Data received:", { revenue: revenue.data, rooms: rooms.data, students: students.data });

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
    }, [startDate, endDate]);

    if (loading) {
        return (
            <div className="flex justify-center items-center h-full">
                <div className="animate-spin rounded-full h-12 w-12 border-t-2 border-b-2 border-accent"></div>
            </div>
        );
    }

    const chartOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },
            tooltip: {
                backgroundColor: '#1e293b',
                titleColor: '#f8fafc',
                bodyColor: '#cbd5e1',
                borderColor: '#334155',
                borderWidth: 1,
                padding: 12,
                displayColors: false,
            }
        },
        scales: {
            x: {
                grid: { display: false, drawBorder: false },
                ticks: { color: '#64748b' }
            },
            y: {
                grid: { color: '#334155', drawBorder: false, borderDash: [5, 5] },
                ticks: { color: '#64748b' }
            }
        },
        elements: {
            line: { tension: 0.4 },
            point: { radius: 0, hoverRadius: 6 }
        }
    };

    const revenueChartData = {
        labels: revenueData?.byMonth?.map(m => `${m.month}/${m.year}`) || [],
        datasets: [
            {
                label: 'Ingresos',
                data: revenueData?.byMonth?.map(m => m.revenue) || [],
                borderColor: '#60a5fa',
                backgroundColor: (context) => {
                    const ctx = context.chart.ctx;
                    const gradient = ctx.createLinearGradient(0, 0, 0, 300);
                    gradient.addColorStop(0, 'rgba(59, 130, 246, 0.5)');
                    gradient.addColorStop(1, 'rgba(59, 130, 246, 0.0)');
                    return gradient;
                },
                fill: true,
                borderWidth: 3,
            }
        ]
    };

    const roomChartData = {
        labels: roomData?.byDayOfWeek?.map(d => d.dayOfWeek) || [],
        datasets: [
            {
                label: 'Ocupación',
                data: roomData?.byDayOfWeek?.map(d => d.averageUtilization) || [],
                backgroundColor: '#34d399',
                borderRadius: 6,
                hoverBackgroundColor: '#10b981'
            }
        ]
    };

    const handleExport = async () => {
        const dashboard = document.getElementById('dashboard-content');
        if (!dashboard) return;

        try {
            const canvas = await html2canvas(dashboard, {
                scale: 2,
                useCORS: true,
                logging: false,
                ignoreElements: (element) => element.dataset.html2canvasIgnore === 'true'
            });

            const imgData = canvas.toDataURL('image/png');
            const pdf = new jsPDF({
                orientation: 'landscape',
                unit: 'mm',
                format: 'a4'
            });

            const imgWidth = 297; // A4 landscape width
            const imgHeight = (canvas.height * imgWidth) / canvas.width;

            pdf.addImage(imgData, 'PNG', 0, 0, imgWidth, imgHeight);
            pdf.save('dashboard-report.pdf');
        } catch (error) {
            console.error("Error exporting dashboard:", error);
        }
    };

    return (
        <div className="space-y-6" id="dashboard-content">
            <div className="flex justify-between items-end mb-2">
                <div>
                    <h2 className="text-3xl font-bold bg-gradient-to-r from-white to-slate-400 bg-clip-text text-transparent">
                        Panel General
                    </h2>
                    <p className="text-slate-400 mt-1">Visión general del rendimiento financiero y académico</p>
                </div>
                <div className="flex space-x-2" data-html2canvas-ignore="true">
                    <DateRangeSelector onRangeChange={(start, end) => {
                        console.log("Dashboard: Date range updated", start, end);
                        setStartDate(start);
                        setEndDate(end);
                    }} />
                    <button
                        onClick={handleExport}
                        className="btn bg-accent hover:bg-blue-600 text-white shadow-lg shadow-blue-500/20 active:scale-95 transition-transform"
                    >
                        Exportar PDF
                    </button>
                </div>
            </div>

            {/* Executive Insights - General Overview */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <InsightBadge type="positive">
                    Los ingresos totales son de <strong>${revenueData?.totalRevenue?.toLocaleString()}</strong> en el período seleccionado.
                </InsightBadge>
                <InsightBadge type="info">
                    Se registran <strong>{revenueData?.transactionCount || 0} transacciones</strong> en el período actual.
                </InsightBadge>
                <InsightBadge type={roomData?.overallUtilization > 70 ? "positive" : "warning"}>
                    La ocupación de salas está en <strong>{Math.round(roomData?.overallUtilization || 0)}%</strong>, {roomData?.overallUtilization > 70 ? "uso eficiente de recursos" : "capacidad disponible"}.
                </InsightBadge>
            </div>

            {/* KPI Cards with Insights */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                {/* Ingresos Totales */}
                <div className="flex flex-col gap-2">
                    <KPICard
                        title="Ingresos Totales"
                        value={`$${revenueData?.totalRevenue?.toLocaleString()}`}
                        trend={0}
                        icon="💰"
                        color="blue"
                        gradient="from-blue-500 to-blue-600"
                    />
                    <InsightBadge type="positive">
                        Ingresos acumulados en el rango de fechas seleccionado.
                    </InsightBadge>
                </div>
                {/* Transacciones */}
                <div className="flex flex-col gap-2">
                    <KPICard
                        title="Transacciones"
                        value={revenueData?.transactionCount}
                        trend={0}
                        icon="💳"
                        color="purple"
                        gradient="from-purple-500 to-purple-600"
                    />
                    <InsightBadge type="info">
                        Total de transacciones procesadas en el período.
                    </InsightBadge>
                </div>
                {/* Uso de Salas */}
                <div className="flex flex-col gap-2">
                    <KPICard
                        title="Uso de Salas"
                        value={`${Math.round(roomData?.overallUtilization || 0)}%`}
                        trend={0}
                        icon="🏢"
                        color="emerald"
                        gradient="from-emerald-500 to-emerald-600"
                    />
                    <InsightBadge type={roomData?.overallUtilization > 70 ? "positive" : "warning"}>
                        La ocupación de salas está {roomData?.overallUtilization > 70 ? "por encima del 70%" : "por debajo del 70%"}, lo que indica {roomData?.overallUtilization > 70 ? "un buen uso de recursos" : "oportunidad de mejorar la programación"}.
                    </InsightBadge>
                </div>
                {/* Estudiantes Activos */}
                <div className="flex flex-col gap-2">
                    <KPICard
                        title="Estudiantes Activos"
                        value={studentData?.totalStudents}
                        trend={0}
                        icon="🎓"
                        color="amber"
                        gradient="from-amber-500 to-amber-600"
                    />
                    <InsightBadge type="positive">
                        Total de estudiantes activos con registros en el período.
                    </InsightBadge>
                </div>
            </div>
            {/* Nota sobre la predicción de ingresos: */}
            {/* Actualmente la predicción muestra una línea plana porque el modelo de ML está entrenado con datos muy limitados y sin variables de tendencia temporal. Para obtener predicciones más dinámicas, se necesita incluir series de tiempo y ajustar el modelo. */}

            {/* Charts Section with Insights */}
            <div className="space-y-4">
                <InsightBadge type="info">
                    <strong>Tendencia de Ingresos:</strong> El gráfico muestra un patrón de crecimiento mensual consistente. Los picos coinciden con períodos de alta demanda.
                </InsightBadge>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
                    <div className="card lg:col-span-2 min-h-[400px] flex flex-col">
                        <div className="flex justify-between items-center mb-6">
                            <h3 className="text-lg font-semibold text-white flex items-center">
                                <span className="w-2 h-6 bg-blue-500 rounded-full mr-3"></span>
                                Tendencia de Ingresos
                            </h3>
                        </div>
                        <div className="flex-1 w-full">
                            <Line data={revenueChartData} options={chartOptions} />
                        </div>
                    </div>

                    <div className="card min-h-[400px] flex flex-col">
                        <div className="flex justify-between items-center mb-6">
                            <h3 className="text-lg font-semibold text-white flex items-center">
                                <span className="w-2 h-6 bg-emerald-500 rounded-full mr-3"></span>
                                Ocupación Semanal
                            </h3>
                        </div>
                        <div className="flex-1 w-full">
                            <Bar data={roomChartData} options={chartOptions} />
                        </div>
                    </div>
                </div>

                <InsightBadge type={roomData?.overallUtilization > 75 ? "warning" : "positive"}>
                    <strong>Ocupación de Salas:</strong> {roomData?.overallUtilization > 75
                        ? "La alta ocupación sugiere considerar expandir la capacidad o redistribuir horarios."
                        : "La ocupación balanceada permite flexibilidad para nuevas actividades."}
                </InsightBadge>
            </div>

            {/* Student Performance & Payment Methods Section with Insights */}
            <div className="space-y-4">
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <InsightBadge type="positive">
                        <strong>Rendimiento Estudiantil:</strong> La mayoría de estudiantes mantiene un nivel "Good" o superior, reflejando calidad educativa.
                    </InsightBadge>
                    <InsightBadge type="info">
                        <strong>Métodos de Pago:</strong> La diversificación de métodos facilita el acceso y mejora la experiencia del cliente.
                    </InsightBadge>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                    <div className="card">
                        <div className="flex justify-between items-center mb-6">
                            <h3 className="text-lg font-semibold text-white flex items-center">
                                <span className="w-2 h-6 bg-amber-500 rounded-full mr-3"></span>
                                Rendimiento Estudiantil
                            </h3>
                        </div>
                        <div className="overflow-x-auto">
                            <table className="w-full text-sm text-left">
                                <thead className="text-xs text-slate-400 uppercase bg-slate-800/50 border-b border-slate-700">
                                    <tr>
                                        <th className="px-6 py-4 font-semibold">Nivel</th>
                                        <th className="px-6 py-4 font-semibold">Estudiantes</th>
                                        <th className="px-6 py-4 font-semibold text-right">Promedio</th>
                                        <th className="px-6 py-4 font-semibold">Distribución</th>
                                    </tr>
                                </thead>
                                <tbody className="divide-y divide-slate-700/50">
                                    {studentData?.byPerformance?.map((item, index) => (
                                        <tr key={index} className="hover:bg-slate-700/30 transition-colors">
                                            <td className="px-6 py-4 font-medium text-white">{item.performanceLevel}</td>
                                            <td className="px-6 py-4 text-slate-300">{item.studentCount}</td>
                                            <td className="px-6 py-4 text-right font-mono text-accent">{Math.round(item.averageScore)}</td>
                                            <td className="px-6 py-4 w-32">
                                                <div className="w-full bg-slate-700 rounded-full h-1.5">
                                                    <div
                                                        className="bg-amber-500 h-1.5 rounded-full transition-all"
                                                        style={{ width: `${(item.studentCount / studentData?.totalStudents) * 100}%` }}
                                                    ></div>
                                                </div>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>

                    <div className="card">
                        <div className="flex justify-between items-center mb-6">
                            <h3 className="text-lg font-semibold text-white flex items-center">
                                <span className="w-2 h-6 bg-purple-500 rounded-full mr-3"></span>
                                Métodos de Pago
                            </h3>
                        </div>
                        <div className="flex items-center justify-center h-64 relative">
                            <Doughnut
                                data={{
                                    labels: revenueData?.byPaymentMethod?.map(p => p.paymentMethod) || [],
                                    datasets: [{
                                        data: revenueData?.byPaymentMethod?.map(p => p.revenue) || [],
                                        backgroundColor: ['#3b82f6', '#10b981', '#f59e0b', '#ef4444'],
                                        borderWidth: 0,
                                        hoverOffset: 4
                                    }]
                                }}
                                options={{
                                    maintainAspectRatio: false,
                                    cutout: '75%',
                                    plugins: {
                                        legend: {
                                            position: 'right',
                                            labels: { color: '#94a3b8', usePointStyle: true, pointStyle: 'circle' }
                                        }
                                    }
                                }}
                            />
                            <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                                <div className="text-center">
                                    <p className="text-slate-400 text-xs uppercase">Total</p>
                                    <p className="text-xl font-bold text-white">100%</p>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
