import { useState } from 'react'
import { BrowserRouter as Router, Routes, Route, Link, useLocation } from 'react-router-dom'
import Dashboard from './pages/Dashboard'
import Revenue from './pages/Revenue'
import Customers from './pages/Customers'
import Rooms from './pages/Rooms'
import Students from './pages/Students'
import Reports from './pages/Reports'

function NavLink({ to, children, icon }) {
    const location = useLocation();
    const isActive = location.pathname === to;

    return (
        <Link
            to={to}
            className={`flex items-center space-x-3 px-4 py-3 rounded-lg transition-colors ${isActive ? 'bg-accent text-white' : 'text-slate-400 hover:bg-slate-800 hover:text-white'
                }`}
        >
            <span>{icon}</span>
            <span>{children}</span>
        </Link>
    );
}

function Layout({ children }) {
    return (
        <div className="flex h-screen bg-primary text-white overflow-hidden">
            {/* Sidebar */}
            <div className="w-64 bg-secondary border-r border-slate-700 flex flex-col">
                <div className="p-6 border-b border-slate-700">
                    <h1 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-purple-500 bg-clip-text text-transparent">
                        FinAI Analytics
                    </h1>
                    <p className="text-xs text-slate-500 mt-1">Financial Intelligence</p>
                </div>

                <nav className="flex-1 p-4 space-y-2 overflow-y-auto">
                    <NavLink to="/" icon="📊">Dashboard</NavLink>
                    <NavLink to="/revenue" icon="💰">Ingresos</NavLink>
                    <NavLink to="/customers" icon="👥">Clientes</NavLink>
                    <NavLink to="/rooms" icon="🏢">Salas</NavLink>
                    <NavLink to="/students" icon="🎓">Estudiantes</NavLink>
                    <NavLink to="/reports" icon="📄">Informes</NavLink>
                </nav>

                <div className="p-4 border-t border-slate-700">
                    <div className="flex items-center space-x-3">
                        <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-accent to-purple-500 flex items-center justify-center font-bold text-sm">
                            JP
                        </div>
                        <div>
                            <p className="text-sm font-medium">Juan Pérez</p>
                            <p className="text-xs text-slate-500">Admin</p>
                        </div>
                    </div>
                </div>
            </div>

            {/* Main Content */}
            <div className="flex-1 flex flex-col overflow-hidden">
                <header className="h-16 bg-secondary border-b border-slate-700 flex items-center justify-between px-8">
                    <h2 className="text-lg font-medium text-slate-200">Panel de Control</h2>
                    <div className="flex items-center space-x-4">
                        <button className="p-2 text-slate-400 hover:text-white">🔔</button>
                        <button className="p-2 text-slate-400 hover:text-white">⚙️</button>
                    </div>
                </header>

                <main className="flex-1 overflow-y-auto p-8 bg-primary">
                    {children}
                </main>
            </div>
        </div>
    );
}

function App() {
    return (
        <Router>
            <Layout>
                <Routes>
                    <Route path="/" element={<Dashboard />} />
                    <Route path="/revenue" element={<Revenue />} />
                    <Route path="/customers" element={<Customers />} />
                    <Route path="/rooms" element={<Rooms />} />
                    <Route path="/students" element={<Students />} />
                    <Route path="/reports" element={<Reports />} />
                </Routes>
            </Layout>
        </Router>
    )
}

export default App
