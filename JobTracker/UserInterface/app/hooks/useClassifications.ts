import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { Classification } from '../types/classifications/classification';
import { CreateClassificationRequest } from '../types/classifications/create-classification-request';
import { CreateClassificationResponse } from '../types/classifications/create-classification-response';
import { DeleteClassificationRequest } from '../types/classifications/delete-classification-request';
import { DeleteClassificationResponse } from '../types/classifications/delete-classification-response';

export const useClassifications = () => {
  return useQuery({
    queryKey: ['classifications'],
    queryFn: () => sendPhotinoRequest<{ Classifications: Classification[] }>('classifications.get', { hello: "hello" }),
    select: (data) => data.Classifications,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useCreateClassification = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: CreateClassificationRequest) => 
      sendPhotinoRequest<CreateClassificationResponse>('classifications.create', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classifications'] });
    },
  });
};

export const useDeleteClassification = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: DeleteClassificationRequest) => 
      sendPhotinoRequest<DeleteClassificationResponse>('classifications.delete', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['classifications'] });
    },
  });
};