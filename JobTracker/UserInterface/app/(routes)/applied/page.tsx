'use client';

import React, { useEffect, useState } from "react";
import { JobApplication, ApplicationStatus } from "@/app/types/applications/jobApplication";
import { GetApplicationResponse } from "@/app/types/applications/get-application";
import { UpdateApplicationRequest } from "@/app/types/applications/update-application";
import { sendPhotinoRequest } from "@/app/utils/photino";
import ApplicationJobPosting from "../../features/search/ApplicationJobPosting";
import { IconAlertCircle, IconBriefcase } from "@tabler/icons-react";

export default function JobApplicationsPage() {
  const [applications, setApplications] = useState<JobApplication[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const loadApplications = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await sendPhotinoRequest("applications.get", {});

      const data = typeof response === 'string' ? JSON.parse(response) : response;
      const applicationResponse: GetApplicationResponse = data;

      console.log(response);

      setApplications(applicationResponse.AppliedJobs || []);
    } catch (err) {
      console.error('Load applications error:', err);
      setError('Failed to load applications. Please try again.');
      setApplications([]);
    } finally {
      setLoading(false);
    }
  };

  const handleStatusChange = async (applicationId: number, newStatus: ApplicationStatus) => {
    try {
      const request: UpdateApplicationRequest = {
        Id: applicationId,
        ApplicationStatus: newStatus
      };

      console.log('Update Status Request:', request);

      const response = await sendPhotinoRequest("applications.update", request);

      console.log('Update Status Response:', response);

      // Update local state
      setApplications(prev => 
        prev.map(app => 
          app.JobId === applicationId 
            ? { ...app, Status: newStatus, LastStatusChangedAt: new Date() }
            : app
        )
      );

      console.log("Status updated successfully");
    } catch (err) {
      console.error('Status update failed:', err);
    }
  };

  const handleDelete = async (jobId: number) => {
    try {
      console.log('Delete Application Request:', { JobId: jobId });

      const response = await sendPhotinoRequest("applications.delete", { JobId: jobId });

      console.log('Delete Application Response:', response);

      // Remove from local state
      setApplications(prev => prev.filter(app => app.JobId !== jobId));

      console.log("Application deleted successfully");
    } catch (err) {
      console.error('Delete application failed:', err);
    }
  };

  useEffect(() => {
    loadApplications();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <div className="p-8">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header and Search Form */}
        <div className="py-6">
          <div className="flex items-start justify-between">
            <div>
              <h1 className="text-2xl font-bold text-neutral-200 mb-2">JOB APPLICATIONS</h1>
              <p className="text-neutral-400">Track and manage all your job applications in one place.</p>
            </div>
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
        {!loading && !error && applications.length === 0 && (
          <div className="card p-8 text-center">
            <div className="w-16 h-16 bg-neutral-800 rounded-full flex items-center justify-center mx-auto mb-4">
              <IconBriefcase className="w-8 h-8 text-neutral-400" />
            </div>
            <h3 className="text-white text-lg font-semibold mb-2">No Applications Yet</h3>
            <p className="text-neutral-400 mb-4">
              You haven't applied to any jobs yet. Start searching and applying to see your applications here.
            </p>
          </div>
        )}

        {/* Application Results */}
        {!loading && applications.length > 0 && (
          <div className="space-y-4">
            {applications.map((application, index) => (
              <ApplicationJobPosting 
                key={application.Id || `${application.JobId}-${index}`} 
                application={application} 
                onStatusChange={handleStatusChange}
                onDelete={handleDelete}
              />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
