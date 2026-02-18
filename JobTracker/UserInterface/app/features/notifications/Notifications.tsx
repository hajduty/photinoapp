'use client';

import React, { useState, useEffect } from 'react';
import {
  Popover,
  ActionIcon,
  Badge,
  Text,
  Stack,
  Group,
  Divider,
  ScrollArea,
  Loader,
  Button,
  Box
} from '@mantine/core';
import { IconBell, IconX, IconCheck } from '@tabler/icons-react';
import { sendPhotinoRequest, onPhotinoEvent } from '../../utils/photino';
import { Notification } from '../../types/notifications/notification';
import { NotificationType } from '../../types/notifications/notification-type';
import { DeleteNotificationRequest } from '../../types/notifications/delete-notification-request';
import { DeleteNotificationResponse } from '../../types/notifications/delete-notification-response';
import { UpdateNotificationRequest } from '../../types/notifications/update-notification-request';
import { UpdateNotificationResponse } from '../../types/notifications/update-notification-response';

export default function Notifications() {
  const [opened, setOpened] = useState(false);
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Fetch notifications when popup opens
  useEffect(() => {
    if (opened) {
      fetchNotifications();
    }
  }, [opened]);

  useEffect(() => {
    if (!opened && notifications.length > 0) {
      const unreadNotifications = notifications.filter(n => !n.IsRead);
      if (unreadNotifications.length > 0) {
        markNotificationsAsRead(unreadNotifications);
      }
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [opened]);

  useEffect(() => {
    const unsubscribe = onPhotinoEvent('notification.new', (newNotification: Notification) => {
      console.log('[Notifications] New notification received:', newNotification);
      setNotifications(prev => [newNotification, ...prev]);
    });

    return () => {
      unsubscribe();
    };
  }, []);

  const fetchNotifications = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await sendPhotinoRequest<Notification[]>('notification.getNotifications', {});
      const sorted = (response || []).sort((a, b) => 
        new Date(b.CreatedAt).getTime() - new Date(a.CreatedAt).getTime()
      );
      setNotifications(sorted);
    } catch (err) {
      console.error('Failed to fetch notifications:', err);
      setError('Failed to load notifications');
    } finally {
      setLoading(false);
    }
  };

  const markNotificationsAsRead = async (unreadNotifications: Notification[]) => {
    try {
      // Mark all unread notifications as read
      await Promise.all(
        unreadNotifications.map(n => 
          sendPhotinoRequest<UpdateNotificationResponse>('notification.updateNotification', {
            Id: n.Id,
            IsRead: true
          } as UpdateNotificationRequest)
        )
      );
      
      // Update local state to reflect read status
      setNotifications(prev => 
        prev.map(n => ({ ...n, IsRead: true }))
      );
    } catch (err) {
      console.error('Failed to mark notifications as read:', err);
    }
  };

  const handleDeleteNotification = async (notificationId: number) => {
    try {
      const request: DeleteNotificationRequest = {
        NotificationId: notificationId
      };

      await sendPhotinoRequest<DeleteNotificationResponse>('notification.deleteNotification', request);
      
      // Remove the notification from the list
      setNotifications(prev => prev.filter(n => n.Id !== notificationId));
    } catch (err) {
      console.error('Failed to delete notification:', err);
    }
  };

  const handleClearAll = async () => {
    try {
      // Delete all notifications one by one
      await Promise.all(
        notifications.map(n => 
          sendPhotinoRequest<DeleteNotificationResponse>('notification.deleteNotification', {
            NotificationId: n.Id
          })
        )
      );
      setNotifications([]);
    } catch (err) {
      console.error('Failed to clear notifications:', err);
    }
  };

  const getNotificationColor = (type: NotificationType) => {
    switch (type) {
      case NotificationType.MatchingJob:
        return 'blue';
      case NotificationType.JobsAdded:
        return 'green';
      default:
        return 'gray';
    }
  };

  const getNotificationTypeLabel = (type: NotificationType) => {
    switch (type) {
      case NotificationType.MatchingJob:
        return 'Matching Job';
      case NotificationType.JobsAdded:
        return 'New Jobs';
      default:
        return 'Notification';
    }
  };

  const unreadCount = notifications.filter(n => !n.IsRead).length;

  return (
    <Popover
      opened={opened}
      onChange={setOpened}
      position="right-start"
      offset={8}
      withArrow
      shadow="md"
    >
      <Popover.Target>
        <ActionIcon
          variant="subtle"
          color="gray"
          size="lg"
          aria-label="Notifications"
          onClick={() => setOpened((o) => !o)}
        >
          <Box pos="relative">
            <IconBell size={20} />
            {unreadCount > 0 && (
              <Badge
                size="xs"
                color="red"
                pos="absolute"
                top={-8}
                right={-8}
                variant="filled"
                styles={{
                  root: {
                    minWidth: '18px',
                    height: '18px',
                    padding: '0 4px',
                    fontSize: '10px',
                  }
                }}
              >
                {unreadCount > 9 ? '9+' : unreadCount}
              </Badge>
            )}
          </Box>
        </ActionIcon>
      </Popover.Target>

      <Popover.Dropdown
        p={0}
        style={{
          width: '320px',
          backgroundColor: '#171717',
          border: '1px solid #404040',
        }}
      >
        {/* Header */}
        <Group justify="space-between" p="sm" style={{ borderBottom: '1px solid #262626' }}>
          <Text size="sm" fw={600} c="white">
            Notifications
          </Text>
          {notifications.length > 0 && (
            <Button
              variant="subtle"
              size="compact-xs"
              color="gray"
              onClick={handleClearAll}
            >
              Clear all
            </Button>
          )}
        </Group>

        {/* Content */}
        <ScrollArea.Autosize mah={320}>
          {loading ? (
            <Group justify="center" py="xl">
              <Loader size="sm" color="gray" />
            </Group>
          ) : error ? (
            <Stack align="center" py="xl" px="md" gap="xs">
              <Text size="sm" c="red.4">
                {error}
              </Text>
              <Button
                variant="subtle"
                size="compact-xs"
                color="gray"
                onClick={fetchNotifications}
              >
                Try again
              </Button>
            </Stack>
          ) : notifications.length === 0 ? (
            <Stack align="center" py="xl" gap="xs">
              <IconCheck size={32} color="#525252" />
              <Text size="sm" c="dimmed">
                NO NOTIFICATIONS
              </Text>
            </Stack>
          ) : (
            <Stack gap={0}>
              {notifications.map((notification, index) => (
                <Box key={notification.Id}>
                  <Group
                    wrap="nowrap"
                    align="flex-start"
                    p="sm"
                    style={{
                      backgroundColor: notification.IsRead ? undefined : 'rgba(59, 130, 246, 0.08)',
                    }}
                    className="notification-item"
                  >
                    <Box mt={4}>
                      <Badge
                        size="xs"
                        color={getNotificationColor(notification.Type)}
                        variant="filled"
                        circle
                        styles={{
                          root: {
                            width: '8px',
                            height: '8px',
                            minWidth: '8px',
                            padding: 0,
                          }
                        }}
                      />
                    </Box>
                    <Stack gap={4} flex={1} style={{ minWidth: 0 }}>
                      <Badge
                        size="xs"
                        color="gray"
                        variant="light"
                        styles={{
                          root: {
                            backgroundColor: 'transparent',
                            padding: 0,
                            fontWeight: 500,
                            textTransform: 'none',
                            alignSelf: 'flex-start',
                          },
                          label: {
                            color: '#737373',
                            fontSize: '10px',
                          }
                        }}
                      >
                        {getNotificationTypeLabel(notification.Type)}
                      </Badge>
                      <Text 
                        size="sm" 
                        fw={notification.IsRead ? 400 : 600} 
                        c="white" 
                        truncate
                      >
                        {notification.Title}
                      </Text>
                      <Text size="xs" c="dimmed" lineClamp={2}>
                        {notification.Description}
                      </Text>
                      <Text size="xs" c="dimmed" mt={2}>
                        {notification.CreatedAt instanceof Date 
                          ? notification.CreatedAt.toLocaleString(undefined, { 
                              month: 'short', 
                              day: 'numeric', 
                              hour: 'numeric', 
                              minute: '2-digit' 
                            })
                          : new Date(notification.CreatedAt).toLocaleString(undefined, { 
                              month: 'short', 
                              day: 'numeric', 
                              hour: 'numeric', 
                              minute: '2-digit' 
                            })
                        }
                      </Text>
                    </Stack>
                    <ActionIcon
                      variant="subtle"
                      color="gray"
                      size="sm"
                      onClick={() => handleDeleteNotification(notification.Id)}
                      style={{
                        opacity: 0,
                        transition: 'opacity 0.15s ease',
                      }}
                      className="dismiss-button"
                    >
                      <IconX size={14} />
                    </ActionIcon>
                  </Group>
                  {index < notifications.length - 1 && (
                    <Divider color="dark.6" />
                  )}
                </Box>
              ))}
            </Stack>
          )}
        </ScrollArea.Autosize>

        {/* Footer */}
        {notifications.length > 0 && (
          <Group justify="center" p="xs" style={{ borderTop: '1px solid #262626' }}>
            <Text size="xs" c="dimmed">
              {notifications.length} notification{notifications.length !== 1 ? 's' : ''}
              {unreadCount > 0 && (
                <Text span size="xs" c="blue.4" ml={4}>
                  ({unreadCount} unread)
                </Text>
              )}
            </Text>
          </Group>
        )}
      </Popover.Dropdown>

      <style jsx global>{`
        .notification-item:hover {
          background-color: #262626 !important;
        }
        .notification-item:hover .dismiss-button {
          opacity: 1 !important;
        }
      `}</style>
    </Popover>
  );
}
