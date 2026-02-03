'use client';

import React, { useState } from 'react';
import JobPosting from '../components/job-posting';
import { Posting } from '../types/posting';
import { sendPhotinoRequest } from '../utils/photino';
import { JobSearchResponse } from '../types/jobsearch';

export default function JobSearch() {
  const [searchTerm, setSearchTerm] = useState('');
  const [jobPostings, setJobPostings] = useState<JobSearchResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(0);
  const [hasMore, setHasMore] = useState(false);
  const ITEMS_PER_PAGE = 20;

  const searchJobs = async (e?: React.FormEvent, page: number = 0) => {
    e?.preventDefault();

    const keyword = searchTerm.trim();
    if (keyword.length < 3) {
      setJobPostings(null);
      setHasMore(false);
      setCurrentPage(0);
      return;
    }

    setLoading(true);
    setError(null);

    try {

      const response = await sendPhotinoRequest("jobSearch.getJobs", {
        keyword,
        limit: ITEMS_PER_PAGE + 1,
        offset: page * ITEMS_PER_PAGE
      });

      const data = typeof response === 'string' ? JSON.parse(response) : response;
      
      // Handle JobSearchResponse format
      if (data && typeof data === 'object' && 'Jobs' in data) {
        const jobSearchResponse: JobSearchResponse = data;
        const jobs = jobSearchResponse.Jobs || [];
        
        // Check if there are more results by checking if we got the full limit
        const hasMoreResults = jobs.length > ITEMS_PER_PAGE;
        
        if (hasMoreResults) {
          setHasMore(true);
          setJobPostings({
            TotalCount: jobSearchResponse.TotalCount,
            Jobs: jobs.slice(0, ITEMS_PER_PAGE)
          });
        } else {
          setHasMore(false);
          setJobPostings(jobSearchResponse);
        }
      } else {
        // Fallback for old format or error case
        const jobs = Array.isArray(data) ? data : [];
        setHasMore(false);
        setJobPostings({
          TotalCount: jobs.length,
          Jobs: jobs
        });
      }

      setCurrentPage(page);
    } catch (err) {
      console.error('Search error:', err);
      setError('Failed to search. Please try again.');
      setJobPostings(null);
      setHasMore(false);
    } finally {
      setLoading(false);
    }
  };

  const handlePageChange = (newPage: number) => {
    if (newPage < 0) return;
    searchJobs(undefined, newPage);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  return (
    <div className="p-8">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header */}
        <div className="py-6">
          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-2xl font-bold text-neutral-200 mb-2">JOB SEARCH</h1>
              <p className="text-neutral-400">Find current job postings on LinkedIn, Arbetsförmedlingen & Indeed</p>
            </div>
          </div>
        </div>

        {/* Search Form */}
        <div className="card p-6">
          <div className="section-header">Search Parameters</div>
          <form onSubmit={searchJobs} className="space-y-4">
            <div className="grid grid-cols-1 lg:grid-cols-4 gap-4">
              <div className="lg:col-span-4">
                <div className="relative">
                  <input
                    type="text"
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    placeholder="Search for jobs... (e.g., developer, designer, manager)"
                    className="input-field w-full pr-32"
                    disabled={loading}
                  />
                  <div className="absolute right-0 top-0 bottom-0 flex gap-1">
                    <button
                      type="submit"
                      disabled={loading || searchTerm.trim().length < 3}
                      className="h-full px-3 py-2 text-neutral-300 hover:text-white border-l border-neutral-600/30 bg-transparent hover:bg-neutral-800/50 rounded-l-none"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                      </svg>
                    </button>
                    <button
                      type="button"
                      onClick={() => {
                        setSearchTerm('');
                        setJobPostings(null);
                        setCurrentPage(0);
                        setHasMore(false);
                      }}
                      className="h-full px-3 py-2 text-neutral-300 hover:text-white border-l border-neutral-600/30 bg-transparent hover:bg-neutral-800/50 rounded-r-none"
                    >
                      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                </div>
                {searchTerm.trim().length > 0 && searchTerm.trim().length < 3 && (
                  <p className="text-sm text-neutral-400 mt-2">
                    Please enter at least 3 characters
                  </p>
                )}
              </div>
            </div>
          </form>
        </div>

        {/* Results Header */}
        {!loading && jobPostings && jobPostings.Jobs.length > 0 && (
          <div className="text-white font-semibold mb-2">
            {jobPostings.TotalCount} job{jobPostings.TotalCount !== 1 ? 's' : ''} found
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
          <div className="card p-8">
            <div className="flex items-center justify-center gap-4">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-400"></div>
              <div>
                <h3 className="text-white font-medium">Searching for jobs...</h3>
              </div>
            </div>
          </div>
        )}

        {/* No Results State */}
        {!loading && !error && jobPostings && jobPostings.Jobs.length === 0 && searchTerm.trim().length >= 3 && (
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
              <button onClick={() => setSearchTerm('developer')} className="btn-secondary">
                Try "developer"
              </button>
              <button onClick={() => setSearchTerm('manager')} className="btn-secondary">
                Try "manager"
              </button>
            </div>
          </div>
        )}

        {/* Job Results */}
        {!loading && jobPostings && jobPostings.Jobs.length > 0 && (
          <div className="space-y-4">
            {jobPostings.Jobs.map((posting, index) => (
              <JobPosting key={posting.Id || `${posting.Id}-${index}`} posting={posting} />
            ))}
          </div>
        )}

        {/* Pagination */}
        {(currentPage > 0 || hasMore) && (
          <div className="card p-6">
            <div className="flex items-center justify-between">
              <div className="flex gap-2">
                <button
                  onClick={() => handlePageChange(currentPage - 1)}
                  disabled={currentPage === 0 || loading}
                  className={`pagination-btn ${currentPage === 0 || loading ? 'opacity-50 cursor-not-allowed' : ''}`}
                >
                  ← Previous
                </button>
                <button
                  onClick={() => handlePageChange(currentPage + 1)}
                  disabled={!hasMore || loading}
                  className={`pagination-btn ${!hasMore || loading ? 'opacity-50 cursor-not-allowed' : ''}`}
                >
                  Next →
                </button>
              </div>

              <div className="text-sm text-neutral-400">
                Page {currentPage + 1} • {hasMore ? 'More results available' : 'End of results'}
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
