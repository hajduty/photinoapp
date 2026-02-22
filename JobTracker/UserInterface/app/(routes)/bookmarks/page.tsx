'use client';

import React, { useEffect, useState } from "react";
import { ExtendedPosting } from "@/app/types/jobs/extended-posting";
import { GetBookmarkedJobsResponse } from "@/app/types/jobs/get-bookmarked-jobs-response";
import { CreateApplicationRequest } from "@/app/types/applications/create-application";
import { sendPhotinoRequest } from "@/app/utils/photino";
import JobPosting from "../../features/search/JobPosting";
import { Pagination, Text, Group, Box } from "@mantine/core";
import { IconAlertCircle, IconBookmarkOff } from "@tabler/icons-react";

export default function SavedPage() {
  const [postings, setPostings] = useState<ExtendedPosting[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentPage, setCurrentPage] = useState(0);

  const ITEMS_PER_PAGE = 20;

    const handleBookmark = async (id: number, targetState: boolean) => {
      try {
        const updatedPosting = await sendPhotinoRequest('jobs.bookmark', {
          PostingId: id,
          IsBookmarked: targetState
        });
  
        setPostings(prev => {
          if (!prev) return prev;
  
          return prev.map(item => {
            if (item.Posting.Id === id) {
              return {
                ...item,
                Posting: { ...updatedPosting.Posting }
              };
            }
            return item;
          });
        });
    
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

  const loadBookmarks = async (page: number = 1) => {
    setLoading(true);
    setError(null);

    try {
      const response = await sendPhotinoRequest("jobs.getBookmarked", {hello:"hello"});

      const data = typeof response === 'string' ? JSON.parse(response) : response;
      const bookmarkResponse: GetBookmarkedJobsResponse = data;

      console.log(response);

      setPostings(bookmarkResponse.TaggedPostings || []);
      setCurrentPage(page - 1);
    } catch (err) {
      console.error('Load bookmarks error:', err);
      setError('Failed to load bookmarks. Please try again.');
      setPostings([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadBookmarks();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <div className="p-4 md:p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        {/* Header and Search Form */}
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-neutral-200 mb-2">SAVED JOBS</h1>
            <p className="text-neutral-400">Your bookmarked jobs.</p>
          </div>
        </div>

        {/* Error State */}
        {error && (
          <div className="card p-6">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 bg-red-500/20 rounded-full flex items-center justify-center">
                <IconAlertCircle className="w-4 h-4 text-red-400" />
              </div>
              <div>
                <h3 className="text-red-400 font-medium">Error</h3>
                <p className="text-neutral-400">{error}</p>
              </div>
            </div>
          </div>
        )}

        {/* No Results State */}
        {!loading && !error && postings.length === 0 && (
          <div className="card p-8 text-center">
            <div className="w-16 h-16 bg-neutral-800 rounded-full flex items-center justify-center mx-auto mb-4">
              <IconBookmarkOff className="w-8 h-8 text-neutral-400" />
            </div>
            <h3 className="text-white text-lg font-semibold mb-2">No Saved Jobs</h3>
            <p className="text-neutral-400 mb-4">
              You haven't saved any jobs yet. Browse jobs and save them to see them here.
            </p>
          </div>
        )}

        {/* Job Results */}
        {!loading && postings.length > 0 && (
          <div className="space-y-4">
            {postings.map((posting, index) => (
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
{/*         {!loading && postings.length > 0 && (
          <Box className="p-6 w-full">
            <Group justify="space-between" align="center">
              <Text size="sm" c="dimmed">
                Page {currentPage + 1} • {postings.length} saved job{postings.length !== 1 ? 's' : ''}
              </Text>
              <Pagination
                total={Math.ceil(postings.length / ITEMS_PER_PAGE)}
                value={currentPage + 1}
                onChange={(page) => {
                  loadBookmarks(page);
                  window.scrollTo({ top: 0, behavior: 'smooth' });
                }}
                disabled={loading}
                color="gray"
                size="md"
                radius="sm"
              />
            </Group>
          </Box>
        )} */}
      </div>
    </div>
  );
}
