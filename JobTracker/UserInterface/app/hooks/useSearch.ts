import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { GetJobsRequest } from '../types/jobs/get-jobs-request';
import { GetJobsResponse } from '../types/jobs/get-jobs-response';
// Note: Semantic search types would need to be defined

export const useJobSearch = (request: GetJobsRequest) => {
  return useQuery({
    queryKey: ['job-search', request],
    queryFn: () => sendPhotinoRequest<GetJobsResponse>('jobs.getJobs', request),
    staleTime: 3 * 60 * 1000, // 3 minutes
  });
};

export const useSemanticSearch = (query: string, minRelevance: number) => {
  return useQuery({
    queryKey: ['semantic-search', query, minRelevance],
    queryFn: () => sendPhotinoRequest('semanticSearch.query', { query, minRelevance }),
    enabled: !!query && query.trim().length >= 3,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useJobTitles = (keyword: string) => {
  return useQuery({
    queryKey: ['job-titles', keyword],
    queryFn: () => sendPhotinoRequest<string[]>('jobs.getTitles', { keyword }),
    enabled: !!keyword && keyword.length >= 2,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
};

export const useCancelSemanticSearch = () => {
  return useMutation({
    mutationFn: () => sendPhotinoRequest('semanticSearch.cancel', {hello:"hello"}),
  });
};