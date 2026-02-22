import React, { useState } from 'react';
import { getContrastColor } from '../../utils/getContrastColor';
import { ExtendedPosting } from '../../types/jobs/extended-posting';
import { IconBolt, IconCalendarTime, IconClock, IconLocation, IconZoom, IconBookmark } from '@tabler/icons-react';
import JobDetailsModal from './JobDetailsModal';

interface JobPostingProps extends ExtendedPosting {
  onBookmark: (targetState: boolean) => void;
  onApply?: (postingId: number) => void;
  onClick?: () => void;
}

export default function JobPosting({ Posting, Tags, onBookmark, onApply, onClick }: JobPostingProps) {
  const active = Posting.Bookmarked;
  const [modalOpened, setModalOpened] = useState(false);

  const handleApply = (e: React.MouseEvent) => {
    e.stopPropagation();
    window.open(Posting.Url, '_blank', 'noopener,noreferrer');
    if (onApply) {
      onApply(Posting.Id);
    }
  };

  const handleBookmarkClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    onBookmark(!active);
  };

  const handleDetailsClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    window.open(Posting.OriginUrl, '_blank', 'noopener,noreferrer');
  };

  const handleCardClick = () => {
    if (onClick) {
      onClick();
    }
    setModalOpened(true);
  };

  return (
    <>
      <div className="card transition-all duration-300 border-neutral-700 cursor-pointer" onClick={handleCardClick}>
        <div className="flex flex-col p-4 border-neutral-700 hover:bg-neutral-800/30 transition-colors duration-200 gap-4">
          <div className="flex items-start gap-4 flex-1 min-w-0">
            <div className="flex-1 min-w-0">
              <div className="flex justify-between items-start gap-3 mb-2">
                <div className="flex flex-row sm:items-center gap-2 sm:gap-3 min-w-0">
                  {Posting.CompanyImage && (
                    <div className="flex-shrink-0">
                      <img
                        src={Posting.CompanyImage}
                        alt={`${Posting.Company} logo`}
                        className="w-8 h-8 sm:w-10 sm:h-10 object-contain p-1 rounded-sm bg-white"
                      />
                    </div>
                  )}
                  <h3 className="text-base sm:text-lg font-semibold text-white hover:text-neutral-300 cursor-pointer transition-colors truncate">
                    {Posting.Title}
                  </h3>
                </div>
                <button 
                  onClick={handleBookmarkClick}
                  className="flex-shrink-0 p-1.5 text-neutral-500 hover:text-yellow-500 transition-colors"
                >
                  <IconBookmark size={20} fill={active ? "currentColor" : "none"} />
                </button>
              </div>

              <p className="text-neutral-300 font-medium mb-3 text-sm">{Posting.Company}</p>

              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2 sm:gap-4 mb-4 text-xs sm:text-sm text-neutral-400">
                <div className="flex items-center gap-2">
                  <IconLocation size={14} />
                  <span className="truncate">{Posting.Location}</span>
                </div>
                <div className="flex items-center gap-2">
                  <IconCalendarTime size={14} />
                  <span className="truncate">Posted: {new Date(Posting.PostedDate).toLocaleString(undefined, { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })}</span>
                </div>
                <div className="flex items-center gap-2">
                  <IconClock size={14} />
                  <span className="truncate">Last app: {new Date(Posting.LastApplicationDate).toLocaleDateString()}</span>
                </div>
              </div>
              <p className="text-neutral-300 text-sm leading-relaxed line-clamp-2">
                {Posting.Description}
              </p>
            </div>
          </div>

          <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 mt-2">
            <div className="flex flex-wrap gap-2 flex-1">
              {Tags.slice(0, 6).map((tag) => (
                <span
                  key={tag.Id}
                  className="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wide"
                  style={{
                    backgroundColor: tag.Color,
                    color: getContrastColor(tag.Color),
                  }}
                >
                  {tag.Name}
                </span>
              ))}
              {Tags.length > 6 && (
                <span className="text-[10px] text-neutral-500 font-medium self-center">
                  +{Tags.length - 6} more
                </span>
              )}
            </div>

            <div className="flex items-center gap-2 flex-shrink-0">
              <button
                onClick={handleDetailsClick}
                className="px-3 py-1.5 text-xs font-medium btn-ghost hover:text-white border hover:bg-neutral-700 transition-all flex items-center gap-1.5"
              >
                <IconZoom size={14} />
                Details
              </button>
              <button
                onClick={handleApply}
                className="px-4 py-1.5 text-xs font-bold btn-primary rounded transition-all flex items-center gap-1.5 shadow-sm"
              >
                <IconBolt size={14} />
                Apply Now
              </button>
            </div>
          </div>
        </div>
      </div>

      <JobDetailsModal
        posting={{ Posting, Tags }}
        opened={modalOpened}
        onClose={() => setModalOpened(false)}
        onApply={onApply ? () => onApply(Posting.Id) : undefined}
        onBookmark={() => onBookmark(!active)}
        isBookmarked={active}
      />
    </>
  );
}
