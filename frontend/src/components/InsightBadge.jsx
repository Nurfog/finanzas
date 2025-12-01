import React from 'react';

export default function InsightBadge({ type, children }) {
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
        <div className={`px-4 py-3 rounded-lg border flex items-start gap-3 ${colors[type] || colors.info} mb-6`}>
            <span className="text-xl mt-0.5">{icons[type] || icons.info}</span>
            <span className="text-sm font-medium leading-relaxed">{children}</span>
        </div>
    );
}
