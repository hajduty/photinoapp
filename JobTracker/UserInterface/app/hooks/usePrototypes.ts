import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { CreatePrototypeRequest } from '../types/prototypes/create-prototype-request';
import { CreatePrototypeResponse } from '../types/prototypes/create-prototype-response';
import { DeletePrototypeRequest } from '../types/prototypes/delete-prototype-request';
import { DeletePrototypeResponse } from '../types/prototypes/delete-prototype-response';
import { GetPrototypesByClassificationResponse } from '../types/prototypes/get-prototypes-by-classification-response';

export const usePrototypesByClassification = (classificationId: number) => {
  return useQuery({
    queryKey: ['prototypes', classificationId],
    queryFn: () => sendPhotinoRequest<GetPrototypesByClassificationResponse>(
      'prototype.getByClassification',
      { ClassificationId: classificationId }
    ),
    select: (data) => data.Prototypes,
    enabled: !!classificationId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useCreatePrototype = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: CreatePrototypeRequest) => 
      sendPhotinoRequest<CreatePrototypeResponse>('prototype.create', request),
    onSuccess: (data, variables) => {
      // Invalidate prototypes for the specific classification
      queryClient.invalidateQueries({ queryKey: ['prototypes', variables.ClassificationId] });
    },
  });
};

export const useDeletePrototype = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: DeletePrototypeRequest) => 
      sendPhotinoRequest<DeletePrototypeResponse>('prototype.delete', request),
    onSuccess: (data, variables) => {
      // Invalidate prototypes for the specific classification
      queryClient.invalidateQueries({ queryKey: ['prototypes'] });
    },
  });
};