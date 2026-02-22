'use client';

import React, { useEffect, useState } from 'react';
import JobPosting from '../../features/search/JobPosting';
import { sendPhotinoRequest } from '../../utils/photino';
import { SearchAutocomplete } from '../../features/search/SearchAutocomplete';
import Filter from '../../features/search/Filter';
import { Pagination, Text, Group, Box } from '@mantine/core';
import { GetJobsRequest } from '../../types/jobs/get-jobs-request';
import { GetJobsResponse } from '../../types/jobs/get-jobs-response';
import { CreateApplicationRequest } from '../../types/applications/create-application';
import { IconAlertCircle, IconUserSearch } from '@tabler/icons-react';

interface ParentFilters {
  source: string;
  date: Date | null;
  location: string;
  tags: number[];
}

export default function JobSearch() {
  const [searchTerm, setSearchTerm] = useState('');
  const [jobPostings, setJobPostings] = useState<GetJobsResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(0);
  const [filters, setFilters] = useState<ParentFilters>({
    source: '',
    date: null,
    location: '',
    tags: [],
  });

  const ITEMS_PER_PAGE = 20;

  const handleBookmark = async (id: number, targetState: boolean) => {
    try {
      const updatedPosting = await sendPhotinoRequest('jobs.bookmark', {
        PostingId: id,
        IsBookmarked: targetState
      });

      setJobPostings(prev => {
        if (!prev) return prev;

        return {
          ...prev,
          // Creating a brand new array reference
          Postings: prev.Postings.map(item => {
            if (item.Posting.Id === id) {
              // We spread the item and the new Posting to ensure 
              // a new reference is created for the whole ExtendedPosting
              return {
                ...item,
                Posting: { ...updatedPosting.Posting }
              };
            }
            return item;
          })
        };
      });

      console.log(updatedPosting);

      console.log("Update successful");
    } catch (err) {
      console.error('Bookmark failed:', err);
    }
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
    } catch (err) {
      console.error('Apply failed:', err);
    }
  };

  const searchJobs = async (e?: React.FormEvent, page: number = 0, keyword?: string) => {
    e?.preventDefault();

    const term = (keyword ?? searchTerm).trim();

    setLoading(true);
    setError(null);

    try {
      console.log(filters.tags);

      const request: GetJobsRequest = {
        Keyword: term,
        Page: page,
        PageSize: ITEMS_PER_PAGE,
        ActiveTagIds: filters.tags,
        TimeSinceUpload: filters.date
      };

      console.log('Search Request:', request);

      const response = await sendPhotinoRequest("jobs.getJobs", request);

      console.log('Search Response:', response);

      const data = typeof response === 'string' ? JSON.parse(response) : response;
      const jobResponse: GetJobsResponse = data;

      setJobPostings({ ...jobResponse });

      setCurrentPage(page - 1);
    } catch (err) {
      console.error('Search error:', err);
      setError('Failed to search. Please try again.');
      setJobPostings(null);
    } finally {
      setLoading(false);
    }
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
        {!loading && jobPostings && jobPostings.TotalResults > 0 && (
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
                <p className="text-neutral-400">{error}</p>
              </div>
            </div>
          </div>
        )}

        {/* Loading State */}
        {loading && (
          <h3 className="text-white font-medium">Searching for jobs...</h3>
        )}

        {/* No Results State */}
        {!loading && !error && jobPostings && jobPostings.Postings?.length === 0 && searchTerm.trim().length >= 3 && (
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
        {!loading && jobPostings && jobPostings.Postings?.length > 0 && (
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
        {!loading && jobPostings && jobPostings.Postings?.length > 0 && (
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
                disabled={loading}
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
