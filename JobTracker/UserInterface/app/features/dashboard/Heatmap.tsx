'use client';

import React, { useMemo } from 'react';
import { Tooltip, Box } from '@mantine/core';
import { useElementSize } from '@mantine/hooks';

interface CustomHeatmapProps {
  data: Record<string, number>;
  onDateClick: (date: string) => void;
}

export const CustomHeatmap = ({ data, onDateClick }: CustomHeatmapProps) => {
  const { ref, width } = useElementSize();

  const days = useMemo(() => {
    const allDays = [];
    const today = new Date();
    
    // 1. Calculate how many weeks we can fit
    // Box size (14px) + Gap (6px) = 20px per week roughly
    // We'll be conservative and use 22px to account for padding
    const boxSizeWithGap = 20; 
    const numberOfWeeks = Math.max(Math.floor(width / boxSizeWithGap), 10);
    const totalDaysToShow = numberOfWeeks * 7;

    // 2. Center "today"
    // We'll show roughly 2/3 past and 1/3 future, or adjust to your liking
    const daysInPast = Math.floor(totalDaysToShow * 0.75);

    for (let i = -daysInPast; i < (totalDaysToShow - daysInPast); i++) {
      const current = new Date();
      current.setDate(today.getDate() + i);

      const y = current.getFullYear();
      const m = String(current.getMonth() + 1).padStart(2, '0');
      const d = String(current.getDate()).padStart(2, '0');
      const dateStr = `${y}-${m}-${d}`;

      allDays.push({
        date: dateStr,
        count: data[dateStr] || 0,
        isToday: i === 0
      });
    }
    return allDays;
  }, [width, data]);

  const getColor = (count: number, isToday: boolean) => {
    if (isToday && count === 0) return '#404040'; // Highlight today even if empty
    if (count === 0) return '#1f1f1f'; 
    if (count === 1) return '#064e3b';
    if (count === 2) return '#065f46';
    if (count === 3) return '#059669';
    return '#10b981';
  };

  return (
    <Box ref={ref} className="w-full">
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
                border: day.isToday ? '1px solid #fbbf24' : 'none' 
              }}
              className="w-3.5 h-3.5 rounded-[2px] cursor-pointer hover:brightness-125 transition-all shrink-0"
            />
          </Tooltip>
        ))}
      </div>
    </Box>
  );
};