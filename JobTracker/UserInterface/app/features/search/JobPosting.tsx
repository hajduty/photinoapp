import React, {  } from 'react';
import { getContrastColor } from '../../utils/getContrastColor';
import { ExtendedPosting } from '../../types/jobs/extended-posting';
import { IconBolt, IconCalendarTime, IconClock, IconLocation, IconZoom } from '@tabler/icons-react';

export default function JobPosting({ Posting, Tags }: ExtendedPosting) {  
  return (
    <div className="card hover:bg-neutral-800 transition-all duration-300 border-neutral-700">
      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between p-4 border-b border-neutral-700 hover:bg-neutral-800/30 transition-colors duration-200 gap-4">
        <div className="flex items-start gap-4 flex-1 min-w-0">
          {/* Logo */}
          {Posting.CompanyImage && (
            <div className="flex-shrink-0">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img 
                src={Posting.CompanyImage} 
                alt={`${Posting.Company} logo`}
                className="w-12 h-12 sm:w-14 sm:h-14 object-contain bg-white rounded-sm"
              />
            </div>
          )}
          
          {/* Details */}
          <div className="flex-1 min-w-0">
            <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-3 mb-2">
              <h3 className="text-base sm:text-lg font-semibold text-white hover:text-neutral-300 cursor-pointer transition-colors truncate">
                {Posting.Title}
              </h3>
              <span className="badge badge-success self-start sm:self-auto">Active</span>
            </div>
            
            <p className="text-neutral-300 font-medium mb-3 text-sm sm:text-base">{Posting.Company}</p>
            
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2 sm:gap-4 mb-4 text-sm text-neutral-400">
              <div className="flex items-center gap-2">
                <IconLocation size={16}/>
                <span className="truncate">{Posting.Location}</span>
              </div>
              
              <div className="flex items-center gap-2">
                <IconCalendarTime size={16}/>
                <span className="truncate">Posted: {new Date(Posting.PostedDate).toLocaleDateString()}</span>
              </div>
              
              <div className="flex items-center gap-2">
                <IconClock size={16}/>
                <span className="truncate">Last app: {new Date(Posting.LastApplicationDate).toLocaleDateString()}</span>
              </div>
            </div>

            <p className="text-neutral-300 text-sm leading-relaxed line-clamp-3">
              {Posting.Description}
            </p>

            {/* Technology Badges */}
            {(() => {
              if (Tags.length === 0) return null;
              
              const limitedTags = Tags.slice(0, 6);
              const hasMore = Tags.length > 6;
              
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
                      +{Tags.length - 6} more
                    </span>
                  )}
                </div>
              );
            })()}
          </div>
        </div>

        <div className="flex flex-col gap-3 lg:ml-6 lg:flex-shrink-0">
          <div className="flex gap-2 justify-start lg:justify-center">
            <button 
              onClick={() => window.open(Posting.OriginUrl, '_blank', 'noopener,noreferrer')}
              className="btn-secondary text-xs sm:text-sm flex-1 sm:flex-none flex justify-center items-center gap-2 py-2.5 sm:py-2"
            >
              <IconZoom size={16}/>
              View Details
            </button>
            <button
              onClick={() => window.open(Posting.Url, '_blank', 'noopener,noreferrer')}
              className="btn-primary text-xs sm:text-sm flex-1 sm:flex-none flex justify-center items-center gap-2 py-2.5 sm:py-2"
            >
              <IconBolt size={16}/>
              Apply Now
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
