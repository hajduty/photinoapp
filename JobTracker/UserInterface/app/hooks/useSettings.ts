import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { Settings } from '../types/settings/settings';
import { UpdateSettingsRequest } from '../types/settings/update-settings-request';
import { UpdateSettingsResponse } from '../types/settings/update-settings-response';
import { UpdatePreferencesRequest } from '../types/settings/update-preferences-request';
import { UpdatePreferencesResponse } from '../types/settings/update-preferences-response';
import { TestConnectionRequest } from '../types/settings/test-connection-request';
import { TestConnectionResponse } from '../types/settings/test-connection-response';

export const useSettings = () => {
  return useQuery({
    queryKey: ['settings'],
    queryFn: () => sendPhotinoRequest<Settings>('settings.getSettings', { hello: "hello" }),
    staleTime: 10 * 60 * 1000, // 10 minutes
  });
};

export const useUpdateSettings = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: UpdateSettingsRequest) => 
      sendPhotinoRequest<UpdateSettingsResponse>('settings.updateSettings', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] });
    },
  });
};

export const useUpdatePreferences = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: UpdatePreferencesRequest) => 
      sendPhotinoRequest<UpdatePreferencesResponse>('settings.updatePreferences', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] });
    },
  });
};

export const useTestConnection = () => {
  return useMutation({
    mutationFn: (request: TestConnectionRequest) => 
      sendPhotinoRequest<TestConnectionResponse>('settings.testConnection', request),
  });
};