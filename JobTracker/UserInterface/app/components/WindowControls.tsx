'use client';

import { useState, useCallback, useEffect } from 'react';
import { IconMinus, IconMaximize, IconMinimize, IconX } from '@tabler/icons-react';
import { sendPhotinoRequest } from '../utils/photino';

export default function WindowControls() {
  const [isMaximized, setIsMaximized] = useState(false);
  const [isDragging, setIsDragging] = useState(false);

  // Check window state on mount
  //useEffect(() => {
  //}, []);

  const handleMinimize = useCallback(async () => {
    try {
      await sendPhotinoRequest('window.command', {
        action: 'minimize'
      });
    } catch (error) {
      console.error('Failed to minimize window:', error);
    }
  }, []);

  const handleMaximizeRestore = useCallback(async () => {
    try {
      const action = isMaximized ? 'restore' : 'maximize';
      await sendPhotinoRequest('window.command', {
        action
      });
      setIsMaximized(!isMaximized);
    } catch (error) {
      console.error('Failed to maximize/restore window:', error);
    }
  }, [isMaximized]);

  const handleClose = useCallback(async () => {
    try {
      await sendPhotinoRequest('window.command', {
        action: 'close'
      });
    } catch (error) {
      console.error('Failed to close window:', error);
    }
  }, []);

  // Handle drag start
  const handleDragStart = useCallback((e: React.MouseEvent) => {
    if (e.button !== 0 || (e.target as HTMLElement).closest('.window-control-btn')) return;

    setIsDragging(true);

    // Use screen coordinates and mark as start
    sendPhotinoRequest('window.command', {
      action: 'drag',
      x: e.screenX,
      y: e.screenY,
      isStart: true
    });
  }, []);

  // Handle drag move
  useEffect(() => {
    if (!isDragging) return;

    const handleMouseMove = (e: MouseEvent) => {
      sendPhotinoRequest('window.command', {
        action: 'drag',
        x: e.screenX,
        y: e.screenY,
        isStart: false
      });
    };

    const handleMouseUp = () => {
      setIsDragging(false);
    };

    window.addEventListener('mousemove', handleMouseMove);
    window.addEventListener('mouseup', handleMouseUp);

    return () => {
      window.removeEventListener('mousemove', handleMouseMove);
      window.removeEventListener('mouseup', handleMouseUp);
    };
  }, [isDragging]);

  return (
    <div
      className="h-10 bg-neutral-950 border-b border-neutral-800 flex items-center justify-between select-none"
      onMouseDown={handleDragStart}
    >
      {/* Title / Drag Area */}
      <div className="flex-1 px-4 flex items-center">
        <span className="text-sm font-semibold text-neutral-400 tracking-wider">
          JOBTRACKER
        </span>
      </div>

      {/* Window Controls */}
      <div className="flex items-center h-full">
        {/* Minimize Button */}
        <button
          onClick={handleMinimize}
          className="window-control-btn w-12 h-full flex items-center justify-center text-neutral-400 hover:text-white hover:bg-neutral-800 transition-colors"
          title="Minimize"
        >
          <IconMinus size={16} stroke={2} />
        </button>

        {/* Maximize/Restore Button */}
        <button
          onClick={handleMaximizeRestore}
          className="window-control-btn w-12 h-full flex items-center justify-center text-neutral-400 hover:text-white hover:bg-neutral-800 transition-colors"
          title={isMaximized ? "Restore" : "Maximize"}
        >
          {isMaximized ? (
            <IconMinimize size={16} stroke={2} />
          ) : (
            <IconMaximize size={16} stroke={2} />
          )}
        </button>

        {/* Close Button */}
        <button
          onClick={handleClose}
          className="window-control-btn w-12 h-full flex items-center justify-center text-neutral-400 hover:text-white hover:bg-red-600 transition-colors"
          title="Close"
        >
          <IconX size={16} stroke={2} />
        </button>
      </div>
    </div>
  );
}
