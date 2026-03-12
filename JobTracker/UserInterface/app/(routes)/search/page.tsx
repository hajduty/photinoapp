'use client';

import React, { useEffect, useState } from 'react';
import JobPosting from '../../features/search/JobPosting';
import { useJobs, useBookmarkJob } from '../../hooks/useJobs';
import { sendPhotinoRequest } from '../../utils/photino';
import { SearchAutocomplete } from '../../features/search/SearchAutocomplete';
import Filter from '../../features/search/Filter';
import { Pagination, Text, Group, Box } from '@mantine/core';
import { CreateApplicationRequest } from '../../types/applications/create-application';
import { IconAlertCircle, IconUserSearch } from '@tabler/icons-react';
import { useQueryClient } from '@tanstack/react-query';

interface ParentFilters {
  source: string;
  date: Date | null;
  location: string;
  tags: number[];
}

export default function JobSearch() {
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(0);
  const [filters, setFilters] = useState<ParentFilters>({
    source: '',
    date: null,
    location: '',
    tags: [],
  });
  const queryClient = useQueryClient();

  const ITEMS_PER_PAGE = 20;

  // Use TanStack Query hooks
  const request = {
    Keyword: searchTerm.trim(),
    Page: currentPage + 1,
    PageSize: ITEMS_PER_PAGE,
    ActiveTagIds: filters.tags,
    TimeSinceUpload: filters.date
  };

  const { data: jobPostings, isLoading, error } = useJobs(request);
  const bookmarkMutation = useBookmarkJob();

  const handleBookmark = (id: number, targetState: boolean) => {
    bookmarkMutation.mutate({ PostingId: id, IsBookmarked: targetState });
  };

  const handleApply = async (postingId: number) => {
    try {
      const request: CreateApplicationRequest = {
        JobId: postingId,
        CoverLetter: ''
      };

      console.log('Application Request:', request);

      const response = await sendPhotinoRequest("applications.create", request);

      console.log('Application Response:', response);

      console.log("Application created successfully");
      
      // Invalidate the applications query to refresh the data
      queryClient.invalidateQueries({ queryKey: ['applications'] });
    } catch (err) {
      console.error('Apply failed:', err);
    }
  };

  const searchJobs = async (e?: React.FormEvent, page: number = 0, keyword?: string) => {
    e?.preventDefault();
    const term = (keyword ?? searchTerm).trim();
    setCurrentPage(page - 1);
  };

  useEffect(() => {
    searchJobs(undefined, 1, searchTerm);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filters]);

  return (
    <div className="md:p-8 p-4">
      <div className="max-w-7xl mx-auto space-y-8">
        {/* Header and Search Form */}
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-neutral-200 mb-2">JOB SEARCH</h1>
            <p className="text-neutral-400">Explore all available jobs instantly, results come from your tracked searches.</p>
          </div>

          <SearchAutocomplete onChange={(val) => { setSearchTerm(val); searchJobs(undefined, 1, val) }} />
        </div>

        {/* Filter Component */}
        <Filter onFilterChange={setFilters} />

        {/* Results Header */}
        {!isLoading && jobPostings && jobPostings.TotalResults > 0 && (
          <div className="text-white font-semibold mb-2">
            {jobPostings.TotalResults} job{jobPostings.TotalResults !== 1 ? 's' : ''} found
          </div>
        )}

        {/* Error State */}
        {error && (
          <div className="card p-6">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 bg-red-500/20 rounded-full flex items-center justify-center">
                <IconAlertCircle className="w-4 h-4 text-red-400" />
              </div>
              <div>
                <h3 className="text-red-400 font-medium">Search Error</h3>
                <p className="text-neutral-400">{error.message}</p>
              </div>
            </div>
          </div>
        )}

        {/* Loading State */}
        {isLoading && (
          <h3 className="text-white font-medium">Searching for jobs...</h3>
        )}

        {/* No Results State */}
        {!isLoading && !error && jobPostings && jobPostings.Postings?.length === 0 && searchTerm.trim().length >= 3 && (
          <div className="card p-8 text-center">
            <div className="w-16 h-16 bg-neutral-800 rounded-full flex items-center justify-center mx-auto mb-4">
              <IconUserSearch className="w-8 h-8 text-neutral-400" />
            </div>
            <h3 className="text-white text-lg font-semibold mb-2">No Jobs Found</h3>
            <p className="text-neutral-400 mb-4">
              No job postings match your search for "{searchTerm}". Try adjusting your search terms or broaden your criteria.
            </p>
            <div className="flex gap-3 justify-center">
              <button onClick={() => { setSearchTerm('developer'); searchJobs(); }} className="btn-secondary">
                Try "developer"
              </button>
              <button onClick={() => { setSearchTerm('manager'); searchJobs(); }} className="btn-secondary">
                Try "manager"
              </button>
            </div>
          </div>
        )}

        {/* Job Results */}
        {!isLoading && jobPostings && jobPostings.Postings?.length > 0 && (
          <div className="space-y-4">
            {jobPostings.Postings.map((posting, index) => (
              <JobPosting 
                key={posting.Posting.Id || `${posting.Posting.Id}-${index}`} 
                Posting={posting.Posting} 
                Tags={posting.Tags} 
                onBookmark={(e) => handleBookmark(posting.Posting.Id, e.valueOf())} 
                onApply={(id) => handleApply(id)}
              />
            ))}
          </div>
        )}

        {/* Pagination */}
        {!isLoading && jobPostings && jobPostings.Postings?.length > 0 && (
          <Box className="p-6 w-full">
            <Group justify="space-between" align="center">
              <Text size="sm" c="dimmed">
                Page {currentPage + 1} • {jobPostings.TotalResults} job{jobPostings.TotalResults !== 1 ? 's' : ''} found
              </Text>
              <Pagination
                total={Math.ceil(jobPostings.TotalResults / ITEMS_PER_PAGE)}
                value={currentPage + 1}
                onChange={(page) => {
                  searchJobs(undefined, page);
                  window.scrollTo({ top: 0, behavior: 'smooth' });
                }}
                disabled={isLoading}
                size="md"
                radius="sm"
              />
            </Group>
          </Box>
        )}
      </div>
    </div>
  );
}
