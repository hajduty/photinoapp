import { useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';

interface CreateProfileRequest { Name: string }
interface CreateProfileResponse { Profile: { Id: number; Name: string }; Success: boolean }

interface SwitchProfileRequest { ProfileId: number }
interface SwitchProfileResponse { Success: boolean; ActiveProfileId: number }

interface DeleteProfileRequest { ProfileId: number }
interface DeleteProfileResponse { Success: boolean; Error: string | null }

export const useCreateProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateProfileRequest) =>
      sendPhotinoRequest<CreateProfileResponse>('profiles.create', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] });
    },
  });
};

export const useSwitchProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: SwitchProfileRequest) =>
      sendPhotinoRequest<SwitchProfileResponse>('profiles.switch', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] });
      queryClient.invalidateQueries({ queryKey: ['matching-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['ignored-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['rejectedTechKeywords'] });
      queryClient.invalidateQueries({ queryKey: ['bookmarked-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.invalidateQueries({ queryKey: ['trackers'] });
    },
  });
};

export const useDeleteProfile = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: DeleteProfileRequest) =>
      sendPhotinoRequest<DeleteProfileResponse>('profiles.delete', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['settings'] });
      queryClient.invalidateQueries({ queryKey: ['matching-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['ignored-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['rejectedTechKeywords'] });
      queryClient.invalidateQueries({ queryKey: ['bookmarked-jobs'] });
      queryClient.invalidateQueries({ queryKey: ['applications'] });
      queryClient.invalidateQueries({ queryKey: ['trackers'] });
    },
  });
};
