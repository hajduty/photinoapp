'use client';

import React, { useEffect, useState } from 'react';
import JobPosting from './JobPosting';
import { sendPhotinoRequest } from '../utils/photino';
import { SearchAutocomplete } from './SearchAutocomplete';
import Filter from './Filter';
import { Pagination, Text, Group, Box } from '@mantine/core';
import { GetJobsRequest } from '../types/get-jobs-request';
import { GetJobsResponse } from '../types/get-jobs-response';

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

      //const loadData = await sendPhotinoRequest('jobSearch.loadJobs', { keyword: term });

      console.log('Search Request:', request);

      const response = await sendPhotinoRequest("jobSearch.getJobs", request);

      console.log('Search Response:', response);

      const data = typeof response === 'string' ? JSON.parse(response) : response;
      const jobSearchResponse: GetJobsResponse = data;

      // No slicing needed — backend already returns exactly the requested page
      setJobPostings({ ...jobSearchResponse });

      setCurrentPage(page);
    } catch (err) {
      console.error('Search error:', err);
      setError('Failed to search. Please try again.');
      setJobPostings(null);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    searchJobs(undefined, 0, searchTerm);
  }, [filters]);

  return (
    <div className="p-8">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header and Search Form */}
        <div className="py-6">
          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-2xl font-bold text-neutral-200 mb-2">JOB SEARCH</h1>
              <p className="text-neutral-400">Find current job postings on LinkedIn, Arbetsförmedlingen & Indeed</p>
            </div>

            <SearchAutocomplete onChange={(val) => { setSearchTerm(val); searchJobs(undefined, 0, val) }} />
          </div>
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
                <svg className="w-4 h-4 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
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
              <svg className="w-8 h-8 text-neutral-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.172 16.172a4 4 0 015.656 0M9 12h6m-6-4h6m2 5.291A7.962 7.962 0 0112 15c-2.34 0-4.291-1.1-5.291-2.709M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
              </svg>
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
              <JobPosting key={posting.Posting.Id || `${posting.Posting.Id}-${index}`} Posting={posting.Posting} Tags={posting.Tags} />
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
                  searchJobs(undefined, page - 1);
                  window.scrollTo({ top: 0, behavior: 'smooth' });
                }}
                disabled={loading}
                color="gray"
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
