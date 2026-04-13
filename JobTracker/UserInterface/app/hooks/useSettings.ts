import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { GetSettingsResponse } from '../types/settings/get-settings-response';
import { UpdateSettingsRequest } from '../types/settings/update-settings-request';
import { UpdateSettingsResponse } from '../types/settings/update-settings-response';
import { UpdatePreferencesRequest } from '../types/settings/update-preferences-request';
import { UpdatePreferencesResponse } from '../types/settings/update-preferences-response';
import { TestConnectionRequest } from '../types/settings/test-connection-request';
import { TestConnectionResponse } from '../types/settings/test-connection-response';

export const useSettings = () => {
  return useQuery({
    queryKey: ['settings'],
    queryFn: () => sendPhotinoRequest<GetSettingsResponse>('settings.getSettings', {}),
    staleTime: 10 * 60 * 1000,
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
