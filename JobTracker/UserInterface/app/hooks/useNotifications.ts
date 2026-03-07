import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { Notification } from '../types/notifications/notification';
import { DeleteNotificationRequest } from '../types/notifications/delete-notification-request';
import { DeleteNotificationResponse } from '../types/notifications/delete-notification-response';
import { UpdateNotificationRequest } from '../types/notifications/update-notification-request';
import { UpdateNotificationResponse } from '../types/notifications/update-notification-response';

export const useNotifications = () => {
  return useQuery({
    queryKey: ['notifications'],
    queryFn: () => sendPhotinoRequest<Notification[]>('notification.getNotifications', { hello: "hello" }),
    select: (data) => (data || []).sort((a, b) =>
      new Date(b.CreatedAt).getTime() - new Date(a.CreatedAt).getTime()
    ),
    staleTime: 1 * 60 * 1000, // 1 minute
  });
};

export const useDeleteNotification = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: DeleteNotificationRequest) => 
      sendPhotinoRequest<DeleteNotificationResponse>('notification.deleteNotification', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });
};

export const useUpdateNotification = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: UpdateNotificationRequest) => 
      sendPhotinoRequest<UpdateNotificationResponse>('notification.updateNotification', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });
};

export const useDeleteAllNotifications = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: () => {
      // This would need to be implemented as a batch operation
      // For now, we'll handle this in the component
      return Promise.resolve();
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notifications'] });
    },
  });
};