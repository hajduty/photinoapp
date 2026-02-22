import React from 'react';
import { getContrastColor } from '../../utils/getContrastColor';
import { ExtendedPosting } from '../../types/jobs/extended-posting';
import { IconBolt, IconCalendarTime, IconClock, IconLocation, IconZoom, IconBookmark } from '@tabler/icons-react';
import { Modal, Divider } from '@mantine/core';

interface JobDetailsModalProps {
  posting: ExtendedPosting;
  opened: boolean;
  onClose: () => void;
  onApply?: () => void;
  onBookmark?: () => void;
  isBookmarked?: boolean;
}

export default function JobDetailsModal({ 
  posting, 
  opened, 
  onClose, 
  onApply, 
  onBookmark,
  isBookmarked = false 
}: JobDetailsModalProps) {
  const { Posting, Tags } = posting;

  const handleApply = () => {
    if (onApply) {
      onApply();
    }
    window.open(Posting.Url, '_blank', 'noopener,noreferrer');
  };

  const handleViewOriginal = () => {
    window.open(Posting.OriginUrl, '_blank', 'noopener,noreferrer');
  };

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      lockScroll={false}
      title={
        <div className="flex items-center gap-3">
          {Posting.CompanyImage && (
            <img
              src={Posting.CompanyImage}
              alt={`${Posting.Company} logo`}
              className="w-10 h-10 object-contain p-1 rounded-sm bg-white"
            />
          )}
          <div className="flex-1">
            <h2 className="text-lg font-semibold text-white pr-4">{Posting.Title}</h2>
            <p className="text-sm text-neutral-400">{Posting.Company}</p>
          </div>
          {onBookmark && (
            <button 
              onClick={onBookmark}
              className="flex-shrink-0 p-2 text-neutral-500 hover:text-yellow-500 transition-colors"
            >
              <IconBookmark size={24} fill={isBookmarked ? "currentColor" : "none"} />
            </button>
          )}
        </div>
      }
      size="xl"
      centered
    >
      <div className="space-y-4">
        {/* Job Info */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
          <div className="flex items-center gap-2 text-neutral-300">
            <IconLocation size={16} />
            <span>{Posting.Location}</span>
          </div>
          <div className="flex items-center gap-2 text-neutral-300">
            <IconCalendarTime size={16} />
            <span>Posted: {new Date(Posting.PostedDate).toLocaleString()}</span>
          </div>
          <div className="flex items-center gap-2 text-neutral-300">
            <IconClock size={16} />
            <span>Last application: {new Date(Posting.LastApplicationDate).toLocaleDateString()}</span>
          </div>
          {Posting.Source && (
            <div className="text-neutral-300">
              <span className="text-neutral-500">Source: </span>
              {Posting.Source}
            </div>
          )}
        </div>

        <Divider />

        {/* Tags */}
        {Tags.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {Tags.map((tag) => (
              <span
                key={tag.Id}
                className="inline-flex items-center px-2 py-1 rounded text-xs font-bold uppercase tracking-wide"
                style={{
                  backgroundColor: tag.Color,
                  color: getContrastColor(tag.Color),
                }}
              >
                {tag.Name}
              </span>
            ))}
          </div>
        )}

        <Divider />

        {/* Full Description - Scrollable with custom scrollbar */}
        <div className="max-h-[400px] overflow-y-auto custom-scrollbar pr-2">
          <h3 className="text-sm font-semibold text-neutral-300 mb-2">Job Description</h3>
          <div 
            className="text-neutral-300 text-sm whitespace-pre-wrap leading-relaxed"
            style={{ 
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-word'
            }}
          >
            {Posting.Description}
          </div>
        </div>

        <Divider />

        {/* Action Buttons */}
        <div className="flex gap-3 justify-end">
          <button
            onClick={handleViewOriginal}
            className="px-4 py-2 text-sm font-medium btn-ghost hover:text-white border hover:bg-neutral-700 transition-all flex items-center gap-2"
          >
            <IconZoom size={16} />
            View Original
          </button>
          <button
            onClick={handleApply}
            className="px-4 py-2 text-sm font-bold btn-primary rounded transition-all flex items-center gap-2 shadow-sm"
          >
            <IconBolt size={16} />
            Apply Now
          </button>
        </div>
      </div>
    </Modal>
  );
}
