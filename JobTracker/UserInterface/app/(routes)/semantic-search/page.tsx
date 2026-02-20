/* 'use client';

import React, { useState } from 'react';
import { 
  TextInput, 
  Slider, 
  Group, 
  Text, 
  Badge,
  Loader,
  Box,
  Pagination
} from '@mantine/core';
import { 
  IconSearch, 
  IconAlertCircle
} from '@tabler/icons-react';
import { sendPhotinoRequest } from '../../utils/photino';
import JobPosting from '../../features/search/JobPosting';
import { ExtendedPosting } from '../../types/jobs/extended-posting';
import { Tag } from '../../types/tag/tag';

// Types for semantic search results
interface RankedPostingResult {
  Id: number;
  Posting: {
    Id: number;
    Title: string;
    Description: string;
    Company: string;
    Location: string;
    PostedDate: Date;
    Url: string;
    OriginUrl: string;
    CompanyImage: string;
    CreatedAt: Date;
    LastApplicationDate: Date;
    Source: string;
  };
  Score: number;
}

interface SemanticSearchResponse {
  Postings: RankedPostingResult[];
  Page: number;
  PageSize: number;
  TotalResults: number;
  TotalPages: number;
  HasPreviousPage: boolean;
  HasNextPage: boolean;
}

const ITEMS_PER_PAGE = 20;

export default function SemanticSearchPage() {
  const [searchQuery, setSearchQuery] = useState('');
  const [minRelevance, setMinRelevance] = useState(0);
  const [results, setResults] = useState<RankedPostingResult[]>([]);
  const [totalResults, setTotalResults] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasSearched, setHasSearched] = useState(false);
  const [currentPage, setCurrentPage] = useState(0);

  const handleSearch = async (e?: React.FormEvent, page: number = 1, query?: string) => {
    e?.preventDefault();
    const term = query ?? searchQuery;
    if (!term.trim()) return;

    setLoading(true);
    setHasSearched(true);
    setError(null);
    if (query) setSearchQuery(term);

    try {
      const response = await sendPhotinoRequest<SemanticSearchResponse>(
        'semanticSearch.query',
        {
          Keyword: term,
          Page: page,
          PageSize: ITEMS_PER_PAGE
        }
      );
      setResults(response.Postings);
      setTotalResults(response.TotalResults);
      setCurrentPage(response.Page - 1);
    } catch (err) {
      console.error('Semantic search error:', err);
      setError('Failed to search. Please try again.');
      setResults([]);
      setTotalResults(0);
    } finally {
      setLoading(false);
    }
  };

  const getRelevanceBadgeColor = (score: number): string => {
    if (score >= 0.8) return 'green';
    if (score >= 0.5) return 'yellow';
    return 'red';
  };

  const renderResults = results.map((result) => {
    const extendedPosting: ExtendedPosting = {
      Posting: result.Posting,
      Tags: [] as Tag[]
    };
    
    return (
      <div key={result.Posting.Id} className="relative">
        <JobPosting Posting={result.Posting} Tags={[]} />
        <div className="absolute top-4 right-4">
          <Badge 
            color={getRelevanceBadgeColor(result.Score)}
            variant="filled"
            size="lg"
          >
            {Math.round(result.Score * 100)}% match
          </Badge>
        </div>
      </div>
    );
  });

  return (
    <div className="p-8">
      <div className="max-w-7xl mx-auto space-y-6">
        <div className="py-6">
          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-2xl font-bold text-neutral-200 mb-2">AI SEMANTIC SEARCH</h1>
              <p className="text-neutral-400">Search across all your data using natural language</p>
            </div>
          </div>
        </div>

        <form onSubmit={handleSearch}>
          <Group gap="sm">
            <TextInput
              placeholder="Describe what you're looking for..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.currentTarget.value)}
              className="flex-1"
              classNames={{
                input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500'
              }}
            />
            <button 
              type="submit"
              disabled={loading}
              className="btn-secondary flex items-center gap-2"
            >
              <IconSearch size={16} />
              Search
            </button>
          </Group>
        </form>

        <div className="flex items-center gap-4">
          <Text size="sm" c="dimmed" className="whitespace-nowrap">Min Relevance</Text>
          <Slider
            value={minRelevance}
            onChange={setMinRelevance}
            min={0}
            max={100}
            step={5}
            color="gray"
            size="sm"
            className="flex-1"
            mb="xs"
          />
          <Text size="sm" c="white" className="w-10 text-right">{minRelevance}%</Text>
        </div>

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

        {loading && (
          <div className="flex items-center justify-center py-8">
            <Loader />
          </div>
        )}

        {!loading && (
          <>
            {!hasSearched && (
              <div className="card p-8 text-center">
                <div className="w-16 h-16 bg-neutral-800 rounded-full flex items-center justify-center mx-auto mb-4">
                  <IconSearch className="w-8 h-8 text-neutral-400" />
                </div>
                <h3 className="text-white text-lg font-semibold mb-2">Start Your Semantic Search</h3>
                <p className="text-neutral-400">
                  Describe what you're looking for in natural language. 
                  Try "jobs similar to my previous role" or "emails about project X"
                </p>
              </div>
            )}

            {hasSearched && results.length === 0 && !error && (
              <div className="card p-8 text-center">
                <h3 className="text-white text-lg font-semibold mb-2">No Results Found</h3>
                <p className="text-neutral-400">
                  Try adjusting your search query or lowering the relevance threshold
                </p>
              </div>
            )}

            {results.length > 0 && (
              <div>
                <Text size="sm" c="dimmed" mb="md">
                  Found {totalResults} result{totalResults !== 1 ? 's' : ''}
                </Text>
                
                <div className="space-y-4">
                  {renderResults}
                </div>

                <Box className="p-6 w-full">
                  <Group justify="space-between" align="center">
                    <Text size="sm" c="dimmed">
                      Page {currentPage + 1} • {totalResults} result{totalResults !== 1 ? 's' : ''} found
                    </Text>
                    <Pagination
                      total={Math.ceil(totalResults / ITEMS_PER_PAGE)}
                      value={currentPage + 1}
                      onChange={(page) => {
                        handleSearch(undefined, page);
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                      }}
                      disabled={loading}
                      color="gray"
                      size="md"
                      radius="sm"
                    />
                  </Group>
                </Box>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
 */