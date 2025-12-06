import React, { useState, useEffect } from 'react';

const DateRangeSelector = ({ startDate, endDate, onRangeChange, className = '' }) => {
    const [localStartDate, setLocalStartDate] = useState(startDate);
    const [localEndDate, setLocalEndDate] = useState(endDate);
    const [preset, setPreset] = useState('custom');

    useEffect(() => {
        setLocalStartDate(startDate);
        setLocalEndDate(endDate);
    }, [startDate, endDate]);

    const handlePresetChange = (e) => {
        const value = e.target.value;
        console.log("DateRangeSelector: Preset changed to", value);
        setPreset(value);

        const end = new Date();
        const start = new Date();

        if (value === '30') {
            start.setDate(end.getDate() - 30);
        } else if (value === '90') {
            start.setDate(end.getDate() - 90);
        } else if (value === '180') {
            start.setDate(end.getDate() - 180);
        } else if (value === '365') {
            start.setDate(end.getDate() - 365);
        } else if (value === 'thisMonth') {
            start.setDate(1);
        } else if (value === 'thisYear') {
            start.setMonth(0, 1);
        } else {
            return; // Custom
        }

        console.log("DateRangeSelector: Emitting range", start.toISOString(), end.toISOString());
        onRangeChange(start, end);
    };

    const handleDateChange = (type, value) => {
        setPreset('custom');
        const date = new Date(value);

        if (type === 'start') {
            setLocalStartDate(date);
            onRangeChange(date, localEndDate);
        } else {
            setLocalEndDate(date);
            onRangeChange(localStartDate, date);
        }
    };

    const formatDate = (date) => {
        if (!date) return '';
        return date.toISOString().split('T')[0];
    };

    return (
        <div className={`flex flex-wrap items-center gap-4 bg-white p-2 rounded-lg shadow-sm border border-gray-200 ${className}`}>
            <div className="flex items-center gap-2">
                <span className="text-sm text-gray-500 font-medium">Periodo:</span>
                <select
                    value={preset}
                    onChange={handlePresetChange}
                    className="text-sm border-gray-300 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 text-gray-900 p-1.5"
                >
                    <option value="30">Últimos 30 días</option>
                    <option value="90">Últimos 90 días</option>
                    <option value="180">Últimos 6 meses</option>
                    <option value="365">Último año</option>
                    <option value="thisMonth">Este mes</option>
                    <option value="thisYear">Este año</option>
                    <option value="custom">Personalizado</option>
                </select>
            </div>

            <div className="flex items-center gap-2">
                <input
                    type="date"
                    value={formatDate(localStartDate)}
                    onChange={(e) => handleDateChange('start', e.target.value)}
                    className="text-sm border-gray-300 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 text-gray-900 p-1.5"
                />
                <span className="text-gray-400">-</span>
                <input
                    type="date"
                    value={formatDate(localEndDate)}
                    onChange={(e) => handleDateChange('end', e.target.value)}
                    className="text-sm border-gray-300 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500 bg-gray-50 text-gray-900 p-1.5"
                />
            </div>
        </div>
    );
};

export default DateRangeSelector;
