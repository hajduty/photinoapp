'use client';

import React, { useMemo, useRef, useState, useEffect } from 'react';
import { Tooltip } from '@mantine/core';

interface CustomHeatmapProps {
  data: Record<string, number>;
  onDateClick: (date: string) => void;
}

export const CustomHeatmap = ({ data, onDateClick }: CustomHeatmapProps) => {
  const containerRef = useRef<HTMLDivElement>(null);
  const [width, setWidth] = useState<number | null>(null);

  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;

    setWidth(el.getBoundingClientRect().width);

    const observer = new ResizeObserver(entries => {
      setWidth(entries[0].contentRect.width);
    });

    observer.observe(el);
    return () => observer.disconnect();
  }, []);

  const days = useMemo(() => {
    const effectiveWidth = width ?? 600;

    const boxSizeWithGap = 20;
    const numberOfWeeks = Math.max(Math.floor(effectiveWidth / boxSizeWithGap), 10);
    const totalDaysToShow = numberOfWeeks * 7;
    const daysInPast = Math.floor(totalDaysToShow * 0.75);

    const today = new Date();
    const allDays = [];

    for (let i = -daysInPast; i < totalDaysToShow - daysInPast; i++) {
      const current = new Date();
      current.setDate(today.getDate() + i);
      const y = current.getFullYear();
      const m = String(current.getMonth() + 1).padStart(2, '0');
      const d = String(current.getDate()).padStart(2, '0');
      const dateStr = `${y}-${m}-${d}`;
      allDays.push({ date: dateStr, count: data[dateStr] || 0, isToday: i === 0 });
    }
    return allDays;
  }, [width, data]);

  const getColor = (count: number, isToday: boolean) => {
    if (isToday && count === 0) return '#404040';
    if (count === 0) return '#1f1f1f';
    if (count === 1) return '#064e3b';
    if (count === 2) return '#065f46';
    if (count === 3) return '#059669';
    return '#10b981';
  };

  return (
    <div ref={containerRef} className="w-full">
      <div
        className="grid grid-rows-7 grid-flow-col gap-1.5 justify-center"
        style={{ gridAutoColumns: 'min-content' }}
      >
        {days.map((day) => (
          <Tooltip
            key={day.date}
            label={`${day.date}: ${day.count} applications`}
            withArrow
            withinPortal
          >
            <div
              onClick={() => onDateClick(day.date)}
              style={{
                backgroundColor: getColor(day.count, day.isToday),
                border: day.isToday ? '1px solid #fbbf24' : 'none',
              }}
              className="w-3.5 h-3.5 rounded-[2px] cursor-pointer hover:brightness-125 transition-all shrink-0"
            />
          </Tooltip>
        ))}
      </div>
    </div>
  );
};