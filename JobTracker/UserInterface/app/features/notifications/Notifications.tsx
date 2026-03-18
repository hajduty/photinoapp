'use client';

import React, { useState, useEffect } from 'react';
import { Popover } from '@mantine/core';
import { IconBell, IconX, IconCheck } from '@tabler/icons-react';
import { useQueryClient } from '@tanstack/react-query';
import { onPhotinoEvent } from '../../utils/photino';
import { Notification } from '../../types/notifications/notification';
import { NotificationType } from '../../types/notifications/notification-type';
import { useNotifications, useUpdateNotification, useDeleteNotification } from '../../hooks/useNotifications';

export default function Notifications() {
  const [opened, setOpened] = useState(false);
  const queryClient = useQueryClient();

  const { data: notifications = [], isLoading, error } = useNotifications();
  const updateNotificationMutation = useUpdateNotification();
  const deleteNotificationMutation = useDeleteNotification();

  // Mark as read when closing
  useEffect(() => {
    if (opened) return;
    const unread = notifications.filter(n => !n.IsRead);
    if (!unread.length) return;
    Promise.all(
      unread.map(n => updateNotificationMutation.mutateAsync({ Id: n.Id, IsRead: true }))
    ).catch(err => console.error('Failed to mark notifications as read:', err));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [opened]);

  useEffect(() => {
    const unsubscribe = onPhotinoEvent('notification.new', () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    });
    return () => unsubscribe();
  }, [queryClient]);

  const handleDelete = async (id: number) => {
    try {
      await deleteNotificationMutation.mutateAsync({ NotificationId: id });
    } catch (err) {
      console.error('Failed to delete notification:', err);
    }
  };

  const handleClearAll = async () => {
    try {
      await Promise.all(notifications.map(n => deleteNotificationMutation.mutateAsync({ NotificationId: n.Id })));
    } catch (err) {
      console.error('Failed to clear notifications:', err);
    }
  };

  const dotColor = (type: NotificationType) => {
    switch (type) {
      case NotificationType.MatchingJob: return 'bg-blue-400';
      case NotificationType.JobsAdded:  return 'bg-green-400';
      default:                          return 'bg-neutral-500';
    }
  };

  const typeLabel = (type: NotificationType) => {
    switch (type) {
      case NotificationType.MatchingJob: return 'Matching job';
      case NotificationType.JobsAdded:  return 'New jobs';
      default:                          return 'Notification';
    }
  };

  const formatDate = (date: Date | string) =>
    new Date(date).toLocaleString(undefined, {
      month: 'short', day: 'numeric',
      hour: 'numeric', minute: '2-digit',
    });

  const unreadCount = notifications.filter(n => !n.IsRead).length;

  return (
    <Popover
      opened={opened}
      onChange={setOpened}
      position="right-start"
      offset={8}
      shadow="md"
      transitionProps={{ transition: 'fade', duration: 150 }}
    >
      <Popover.Target>
        <button
          onClick={() => setOpened(o => !o)}
          aria-label="Notifications"
          className="relative p-2 rounded-lg text-neutral-400 hover:text-white hover:bg-neutral-800 transition-colors"
        >
          <IconBell size={18} />
          {unreadCount > 0 && (
            <span className="absolute top-1 right-1 flex items-center justify-center w-4 h-4 rounded-full bg-red-500 text-[9px] font-bold text-white leading-none">
              {unreadCount > 9 ? '9+' : unreadCount}
            </span>
          )}
        </button>
      </Popover.Target>

      <Popover.Dropdown p={0} style={{ width: 300, background: 'transparent', border: 'none', boxShadow: 'none' }}>
        <div className="w-[300px] rounded-xl border border-neutral-800 bg-neutral-900 shadow-xl overflow-hidden">

          {/* Header */}
          <div className="flex items-center justify-between px-3 py-2.5 border-b border-neutral-800">
            <span className="text-[12px] font-medium text-white">Notifications</span>
            {notifications.length > 0 && (
              <button
                onClick={handleClearAll}
                className="text-[11px] text-neutral-400 hover:text-neutral-300 transition-colors"
              >
                Clear all
              </button>
            )}
          </div>

          {/* Content */}
          <div className="max-h-[320px] overflow-y-auto custom-scrollbar">
            {isLoading ? (
              <div className="flex items-center justify-center py-10">
                <div className="w-5 h-5 rounded-full border-2 border-neutral-700 border-t-neutral-400 animate-spin" />
              </div>
            ) : error ? (
              <div className="flex flex-col items-center gap-2 py-10 px-4">
                <p className="text-[12px] text-red-400">{error.message}</p>
              </div>
            ) : notifications.length === 0 ? (
              <div className="flex flex-col items-center gap-2 py-10">
                <IconCheck size={28} className="text-neutral-700" />
                <p className="text-[11px] font-medium text-neutral-500 uppercase tracking-widest">No notifications</p>
              </div>
            ) : (
              <div className="divide-y divide-neutral-800/60">
                {notifications.map((notification) => (
                  <div
                    key={notification.Id}
                    className={`group flex items-start gap-2.5 px-3 py-2.5 transition-colors hover:bg-neutral-900 ${
                      !notification.IsRead ? 'bg-blue-500/5' : ''
                    }`}
                  >
                    <div className="flex-shrink-0 mt-1.5">
                      <div className={`w-1.5 h-1.5 rounded-full ${dotColor(notification.Type)}`} />
                    </div>

                    <div className="flex-1 min-w-0 space-y-0.5">
                      <p className="text-[10px] text-neutral-500">{typeLabel(notification.Type)}</p>
                      <p className={`text-[12px] text-white truncate ${notification.IsRead ? 'font-normal' : 'font-medium'}`}>
                        {notification.Title}
                      </p>
                      <p className="text-[11px] text-neutral-500 line-clamp-2 leading-relaxed">
                        {notification.Description}
                      </p>
                      <p className="text-[10px] text-neutral-500 pt-0.5">
                        {formatDate(notification.CreatedAt)}
                      </p>
                    </div>

                    <button
                      onClick={() => handleDelete(notification.Id)}
                      className="flex-shrink-0 p-0.5 mt-0.5 rounded text-neutral-700 hover:text-neutral-400 opacity-0 group-hover:opacity-100 transition-all"
                      aria-label="Dismiss"
                    >
                      <IconX size={13} />
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Footer */}
          {notifications.length > 0 && (
            <div className="flex items-center justify-center px-3 py-2 border-t border-neutral-800">
              <p className="text-[11px] text-neutral-600">
                {notifications.length} notification{notifications.length !== 1 ? 's' : ''}
                {unreadCount > 0 && (
                  <span className="text-blue-500 ml-1">· {unreadCount} unread</span>
                )}
              </p>
            </div>
          )}
        </div>
      </Popover.Dropdown>
    </Popover>
  );
}