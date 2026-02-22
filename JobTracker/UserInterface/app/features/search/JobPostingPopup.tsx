import { HeatmapJobData } from '@/app/types/dashboard/get-heatmapdate-response';
import React from 'react';

interface JobPostingPopupProps {
  job: HeatmapJobData;
}

export default function JobPostingPopup({ job }: JobPostingPopupProps) {
  return (
    <div className="flex items-center gap-3 p-2 min-w-[200px]">
      {job.CompanyImage && (
        <img
          src={job.CompanyImage}
          alt={`${job.Company} logo`}
          className="w-8 h-8 object-contain p-1 rounded-sm bg-white flex-shrink-0"
        />
      )}
      <div className="min-w-0">
        <p className="text-white text-sm font-medium truncate">{job.JobTitle}</p>
        <p className="text-neutral-400 text-xs truncate">{job.Company}</p>
      </div>
    </div>
  );
}
