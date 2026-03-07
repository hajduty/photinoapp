import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { Tag } from '../types/tag/tag';
import { CreateTagRequest } from '../types/tag/create-tag-request';
import { CreateTagResponse } from '../types/tag/create-tag-response';
import { UpdateTagRequest } from '../types/tag/update-tag-request';
import { UpdateTagResponse } from '../types/tag/update-tag-response';
import { DeleteTagRequest } from '../types/tag/delete-tag-request';
import { DeleteTagResponse } from '../types/tag/delete-tag-response';

export const useTags = () => {
  return useQuery({
    queryKey: ['tags'],
    queryFn: () => sendPhotinoRequest<Tag[]>('tags.getTags', { hello: "hello" }),
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useCreateTag = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: CreateTagRequest) => 
      sendPhotinoRequest<CreateTagResponse>('tags.createTag', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
    },
  });
};

export const useUpdateTag = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: UpdateTagRequest) => 
      sendPhotinoRequest<UpdateTagResponse>('tags.updateTag', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
    },
  });
};

export const useDeleteTag = () => {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (request: DeleteTagRequest) => 
      sendPhotinoRequest<DeleteTagResponse>('tags.deleteTag', request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tags'] });
    },
  });
};