import { useQuery } from '@tanstack/react-query';
import { sendPhotinoRequest } from '../utils/photino';
import { Classification } from '../types/classifications/classification';
import { JobSentenceDto } from '../types/jobs/jobsentence';

export const useClassifications = () => {
  return useQuery({
    queryKey: ['classifications'],
    queryFn: () => sendPhotinoRequest<{ Classifications: Classification[] }>('classifications.get', {hello:"hello"}),
    select: (data) => data.Classifications,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

export const useJobSentences = (jobId: number) => {
  return useQuery({
    queryKey: ['jobSentences', jobId],
    queryFn: () => sendPhotinoRequest<{ Sentences: JobSentenceDto[] }>('embeddings.getDescription', { JobId: jobId }),
    select: (data) => data.Sentences,
    enabled: !!jobId,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};
