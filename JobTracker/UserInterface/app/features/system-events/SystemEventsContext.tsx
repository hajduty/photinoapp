'use client';

import React, { createContext, useContext, useEffect, useCallback, useRef, ReactNode } from 'react';
import { notifications } from '@mantine/notifications';
import { onPhotinoEvent, sendPhotinoRequest } from '../../utils/photino';
import { Button } from '@mantine/core';

const SystemEventsContext = createContext<{ onSystemEvent: (handler: (name: string, data: unknown) => void) => () => void } | null>(null);

const embeddingsId = 'embeddings-generating';

function showEmbeddingsToast(data: unknown, onCancel?: () => void) {
  const d = data as Record<string, unknown>;
  const title = (d?.Title as string) || 'Embeddings';
  const desc = (d?.Description as string) || '';
  const progress = d?.progress;
  const message = progress ? `${desc} ${progress}%` : desc || 'Generating...';
  
  notifications.hide(embeddingsId);
  notifications.show({
    id: embeddingsId,
    title,
    message: (
      <div className="flex items-center justify-between w-full gap-8">
        <span>{message}</span>
        {onCancel && <Button size="xs" variant="light" color="red" onClick={onCancel}>Cancel</Button>}
      </div>
    ),
    color: 'blue',
    autoClose: false,
    withCloseButton: false,
  });
}

function showToast(eventName: string, data: unknown) {
  const d = data as Record<string, unknown>;
  const title = (d?.Title as string) || '';
  const message = (d?.Description as string) || '';

  switch (eventName) {
    case 'notification.new':
      notifications.show({ title: title || 'New Jobs Added', message: message || 'New jobs added', color: 'green' });
      break;
    case 'jobs.added':
      notifications.show({ title: title || 'New Jobs Added', message: message || 'New jobs added', color: 'green' });
      break;
    case 'embeddings.started':
    case 'embeddings.generating':
      break; // handled in dispatch
    case 'embeddings.completed':
      notifications.hide(embeddingsId);
      notifications.show({ title: title || 'Complete', message: message || 'Done', color: 'green' });
      break;
    case 'embeddings.error':
      notifications.hide(embeddingsId);
      notifications.show({ title: title || 'Error', message: message || 'Failed', color: 'red' });
      break;
    case 'jobs.tracking.started':
      notifications.show({ title: title || 'Started', message, color: 'blue' });
      break;
    case 'jobs.tracking.completed':
      notifications.show({ title: title || 'Complete', message, color: 'green' });
      break;
    case 'jobs.tracking.error':
      notifications.show({ title: title || 'Error', message, color: 'red' });
      break;
  }
}

export function SystemEventsProvider({ children }: { children: ReactNode }) {
  const handlers = useRef<Set<(name: string, data: unknown) => void>>(new Set());

  const cancelEmbeddings = useCallback(async () => {
    notifications.hide(embeddingsId);
    await sendPhotinoRequest("semanticSearch.cancel", {});
  }, []);

  const dispatch = useCallback((name: string, data: unknown) => {
    if (name === 'embeddings.started' || name === 'embeddings.generating') {
      showEmbeddingsToast(data, cancelEmbeddings);
    } else {
      showToast(name, data);
    }
    handlers.current.forEach(h => h(name, data));
  }, [cancelEmbeddings]);

  useEffect(() => {
    const events = ['notification.new', 'jobs.added', 'embeddings.started', 'embeddings.generating', 'embeddings.completed', 'embeddings.error', 'jobs.tracking.started', 'jobs.tracking.completed', 'jobs.tracking.error'];
    const unsubs = events.map(e => onPhotinoEvent(e, d => dispatch(e, d)));
    return () => unsubs.forEach(u => u());
  }, [dispatch]);

  if (typeof window !== 'undefined') {
    (window as unknown as { cancelEmbeddings: () => void }).cancelEmbeddings = cancelEmbeddings;
  }

  return <SystemEventsContext.Provider value={{ onSystemEvent: (h) => { handlers.current.add(h); return () => handlers.current.delete(h); } }}>{children}</SystemEventsContext.Provider>;
}

export function useSystemEvents() {
  const ctx = useContext(SystemEventsContext);
  if (!ctx) throw new Error('useSystemEvents must be used within SystemEventsProvider');
  return ctx;
}
