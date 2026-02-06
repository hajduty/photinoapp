import React, { useState, useEffect } from 'react';
import { Posting } from '../types/posting';
import { Tag } from '../types/tag';
import { sendPhotinoRequest } from '../utils/photino';
import { getContrastColor } from '../utils/getContrastColor';

interface JobPostingProps {
  posting: Posting;
}

export default function JobPosting({ posting }: JobPostingProps) {
  const [tags, setTags] = useState<Tag[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    fetchTags();
  }, []);

  const fetchTags = async () => {
    try {
      setLoading(true);
      const response = await sendPhotinoRequest<Tag[]>('tags.getTags', { test: "anything" });
      setTags(response);
    } catch (err) {
      console.error('Failed to fetch tags:', err);
    } finally {
      setLoading(false);
    }
  };

  // Function to match tags from description
  const getMatchingTags = (description: string): Tag[] => {
    if (!description || !tags.length) return [];
    
    const foundTags = new Set<Tag>();
    const lowerDescription = description.toLowerCase();
    
    for (const tag of tags) {
      const pattern = new RegExp(`\\b${tag.Name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}\\b`, 'i');
      if (pattern.test(description)) {
        foundTags.add(tag);
      }
    }
    
    return Array.from(foundTags);
  };
  
  return (
    <div className="card hover:bg-neutral-800 transition-all duration-300 border-neutral-700">
      <div className="list-item">
        <div className="flex items-start gap-4 flex-1">
          {/* Logo */}
          {posting.CompanyImage && (
            <div className="flex-shrink-0">
              <img 
                src={posting.CompanyImage} 
                alt={`${posting.Company} logo`}
                className="w-14 h-14 object-contain bg-white rounded-sm"
              />
            </div>
          )}
          
          {/* Details */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-3 mb-2">
              <h3 className="text-lg font-semibold text-white hover:text-neutral-300 cursor-pointer transition-colors truncate">
                {posting.Title}
              </h3>
              <span className="badge badge-success">Active</span>
            </div>
            
            <p className="text-neutral-300 font-medium mb-3">{posting.Company}</p>
            
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-4 text-sm text-neutral-400">
              <div className="flex items-center gap-2">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 11a3 3 0 11-6 0 3 3 0 016 0z" />
                </svg>
                <span>{posting.Location}</span>
              </div>
              
              <div className="flex items-center gap-2">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                </svg>
                <span>Posted: {new Date(posting.PostedDate).toLocaleDateString()}</span>
              </div>
              
              <div className="flex items-center gap-2">
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>Last app: {new Date(posting.LastApplicationDate).toLocaleDateString()}</span>
              </div>
            </div>

            <p className="text-neutral-300 text-sm leading-relaxed line-clamp-3">
              {posting.Description}
            </p>

            {/* Technology Badges */}
            {(() => {
              const matchingTags = getMatchingTags(posting.Description);
              if (matchingTags.length === 0) return null;
              
              // Limit to first 6 tags to avoid overwhelming the UI
              const limitedTags = matchingTags.slice(0, 6);
              const hasMore = matchingTags.length > 6;
              
              return (
                <div className="flex flex-wrap gap-2 mt-3">
                  {limitedTags.map((tag) => (
                    <span
                      key={tag.Id}
                      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium`}
                      style={{
                        backgroundColor: tag.Color,
                        color: getContrastColor(tag.Color),
                        border: 'none'
                      }}
                    >
                      {tag.Name}
                    </span>
                  ))}
                  {hasMore && (
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-800 border border-gray-200">
                      +{matchingTags.length - 6} more
                    </span>
                  )}
                </div>
              );
            })()}
          </div>
        </div>

        <div className="flex flex-col gap-3 ml-6">
            <div className="flex gap-2 justify-center align-middle">
            <button 
              onClick={() => window.open(posting.Url, '_blank', 'noopener,noreferrer')}
              className="btn-secondary text-sm flex justify-center items-center"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
              </svg>
              View Details
            </button>
            <button
              onClick={() => window.open(posting.OriginUrl, '_blank', 'noopener,noreferrer')}
              className="btn-primary text-sm flex justify-center items-center"
            >
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 10V3L4 14h7v7l9-11h-7z" />
              </svg>
              Apply Now
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
