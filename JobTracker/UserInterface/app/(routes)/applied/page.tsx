'use client';

import React, { useState } from "react";
import { ApplicationStatus } from "@/app/types/applications/jobApplication";
import ApplicationJobPosting from "../../features/search/ApplicationJobPosting";
import { IconAlertCircle, IconBriefcase } from "@tabler/icons-react";
import { useApplications, useUpdateApplication, useDeleteApplication } from '../../hooks/useApplications';
import { Pagination, Text, Group, Box } from "@mantine/core";

export default function JobApplicationsPage() {
  const ITEMS_PER_PAGE = 10; // Reduce page size for better performance
  const [currentPage, setCurrentPage] = useState(1);
  
  const { data: applications, isLoading, error } = useApplications();
  const updateApplicationMutation = useUpdateApplication();
  const deleteApplicationMutation = useDeleteApplication();

  const handleStatusChange = (applicationId: number, newStatus: ApplicationStatus) => {
    updateApplicationMutation.mutate({ Id: applicationId, ApplicationStatus: newStatus });
  };

  const handleDelete = (jobId: number) => {
    deleteApplicationMutation.mutate(jobId);
  };

  return (
    <div className="md:p-8 p-4">
      <div className="max-w-7xl mx-auto space-y-6">
        {/* Header and Search Form */}
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold text-neutral-200 mb-2">JOB APPLICATIONS</h1>
            <p className="text-neutral-400">Track and manage all your job applications in one place.</p>
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
                <p className="text-neutral-400">{error.message}</p>
              </div>
            </div>
          </div>
        )}

        {/* No Results State */}
        {!isLoading && !error && (!applications || applications.length === 0) && (
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
        {!isLoading && applications && applications.length > 0 && (
          <div className="space-y-4">
            {applications
              .slice((currentPage - 1) * ITEMS_PER_PAGE, currentPage * ITEMS_PER_PAGE)
              .map((application, index) => (
                <ApplicationJobPosting 
                  key={application.Id || `${application.JobId}-${index}`} 
                  application={application} 
                  onStatusChange={handleStatusChange}
                  onDelete={handleDelete}
                />
              ))}
          </div>
        )}

        {/* Pagination */}
        {!isLoading && applications && applications.length > 0 && (
          <Box className="p-6 w-full">
            <Group justify="space-between" align="center">
              <Text size="sm" c="dimmed">
                Page {currentPage} • {applications.length} applications
              </Text>
              <Pagination
                total={Math.ceil(applications.length / ITEMS_PER_PAGE)}
                value={currentPage}
                onChange={setCurrentPage}
                disabled={isLoading}
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
