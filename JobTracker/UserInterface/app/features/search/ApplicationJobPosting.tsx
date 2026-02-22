import React, { useState } from 'react';
import { getContrastColor } from '../../utils/getContrastColor';
import { JobApplication, ApplicationStatus } from '../../types/applications/jobApplication';
import { IconBolt, IconCalendarTime, IconClock, IconLocation, IconZoom, IconCheck, IconX, IconUser, IconGhost, IconMail, IconTrash } from '@tabler/icons-react';
import { Modal, Menu, ActionIcon, Group, Text, Badge, Divider, Paper } from '@mantine/core';

// Mock email data type
interface MockEmail {
  id: number;
  from: string;
  subject: string;
  date: Date;
  body: string;
}

// Mock emails for demonstration
const mockEmails: MockEmail[] = [
  {
    id: 1,
    from: 'hr@company.com',
    subject: 'Application Received',
    date: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000),
    body: 'Thank you for applying to the Software Engineer position. We have received your application and will review it shortly.'
  },
  {
    id: 2,
    from: 'hiring@company.com',
    subject: 'Interview Invitation',
    date: new Date(Date.now() - 1 * 24 * 60 * 60 * 1000),
    body: 'We would like to invite you for an interview. Please let us know your availability for the next week.'
  }
];

// Color mapping for each application status (pastel colors for dark theme)
const statusColors: Record<ApplicationStatus, string> = {
  [ApplicationStatus.Pending]: '#fbbf24',
  [ApplicationStatus.Submitted]: '#60a5fa',
  [ApplicationStatus.Interview]: '#a78bfa',
  [ApplicationStatus.Offer]: '#34d399',
  [ApplicationStatus.Accepted]: '#4ade80',
  [ApplicationStatus.Rejected]: '#f87171',
  [ApplicationStatus.Ghosted]: '#9ca3af',
};

// Label mapping for each application status
const statusLabels: Record<ApplicationStatus, string> = {
  [ApplicationStatus.Pending]: 'Pending',
  [ApplicationStatus.Submitted]: 'Submitted',
  [ApplicationStatus.Interview]: 'Interview',
  [ApplicationStatus.Offer]: 'Offer',
  [ApplicationStatus.Accepted]: 'Accepted',
  [ApplicationStatus.Rejected]: 'Rejected',
  [ApplicationStatus.Ghosted]: 'Ghosted',
};

interface ApplicationJobPostingProps {
  application: JobApplication;
  onStatusChange?: (applicationId: number, newStatus: ApplicationStatus) => void;
  onDelete?: (jobId: number) => void;
}

export default function ApplicationJobPosting({ application, onStatusChange, onDelete }: ApplicationJobPostingProps) {
  const { Posting, Status, AppliedAt, LastStatusChangeAt, CoverLetter } = application;
  const statusColor = statusColors[Status];
  const statusLabel = statusLabels[Status];

  const [emailModalOpened, setEmailModalOpened] = useState(false);
  const [selectedEmail, setSelectedEmail] = useState<MockEmail | null>(null);

  const handleStatusChange = (newStatus: ApplicationStatus) => {
    if (onStatusChange) {
      onStatusChange(application.JobId, newStatus);
    }
  };

  const handleDelete = () => {
    if (onDelete) {
      onDelete(application.JobId);
    }
  };

  return (
    <>
      <div className="card transition-all duration-300 border-neutral-700">
        <div className="flex flex-col p-4 border-neutral-700 hover:bg-neutral-800/30 transition-colors duration-200 gap-4">
          
          {/* Main Content Area */}
          <div className="flex items-start gap-4 flex-1 min-w-0">
            <div className="flex-1 min-w-0">
              
              {/* Header with Status Badge */}
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
                
                {/* Application Status Badge with Mantine Menu */}
                <Menu shadow="md" width={150} position="bottom-end">
                  <Menu.Target>
                    <Badge 
                      size="sm" 
                      radius="xs"
                      variant="filled"
                      style={{ backgroundColor: statusColor, color: '#1f2937', cursor: 'pointer' }}
                    >
                      {statusLabel}
                    </Badge>
                  </Menu.Target>
                  <Menu.Dropdown>
                    {Object.entries(statusLabels).map(([key, label]) => (
                      <Menu.Item 
                        key={key}
                        onClick={() => handleStatusChange(parseInt(key) as ApplicationStatus)}
                        leftSection={Status === parseInt(key) ? <IconCheck size={14} /> : null}
                        style={{ color: statusColors[parseInt(key) as ApplicationStatus] }}
                      >
                        {label}
                      </Menu.Item>
                    ))}
                  </Menu.Dropdown>
                </Menu>
              </div>

              <p className="text-neutral-300 font-medium mb-3 text-sm">{Posting.Company}</p>

              {/* Info Grid */}
              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-2 sm:gap-4 mb-4 text-xs sm:text-sm text-neutral-400">
                <div className="flex items-center gap-2">
                  <IconLocation size={14} />
                  <span className="truncate">{Posting.Location}</span>
                </div>
                <div className="flex items-center gap-2">
                  <IconCalendarTime size={14} />
                  <span className="truncate">Posted: {new Date(Posting.CreatedAt).toLocaleString(undefined, { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })}</span>
                </div>
                <div className="flex items-center gap-2">
                  <IconClock size={14} />
                  <span className="truncate">Applied: {new Date(AppliedAt).toLocaleString(undefined, { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })}</span>
                </div>
                <div className="flex items-center gap-2">
                  <IconClock size={14} />
                  <span className="truncate">Status: {new Date(LastStatusChangeAt).toLocaleString(undefined, { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' })}</span>
                </div>
              </div>
              
              {/* Cover Letter Preview */}
              {CoverLetter && (
                <p className="text-neutral-300 text-sm leading-relaxed line-clamp-2">
                  <span className="text-neutral-500 font-medium">Cover Letter: </span>
                  {CoverLetter}
                </p>
              )}
            </div>
          </div>

          {/* Footer Div */}
          <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 mt-2">
            
            {/* Buttons */}
            <div className="flex items-center gap-2 flex-shrink-0">
              <button
                onClick={() => window.open(Posting.OriginUrl, '_blank', 'noopener,noreferrer')}
                className="px-3 py-1.5 text-xs font-medium btn-ghost hover:text-white border hover:bg-neutral-700 transition-all flex items-center gap-1.5"
              >
                <IconZoom size={14} />
                Details
              </button>
              <button
                onClick={() => window.open(Posting.Url, '_blank', 'noopener,noreferrer')}
                className="px-4 py-1.5 text-xs font-bold btn-primary rounded transition-all flex items-center gap-1.5 shadow-sm"
              >
                <IconBolt size={14} />
                Original Post
              </button>
              {/* Email Button */}
              <button
                onClick={() => setEmailModalOpened(true)}
                className="px-3 py-1.5 text-xs font-medium btn-ghost hover:text-white border hover:bg-neutral-700 transition-all flex items-center gap-1.5"
              >
                <IconMail size={14} />
                Emails
              </button>
              {/* Delete Button */}
              <button
                onClick={handleDelete}
                className="px-3 py-1.5 text-xs font-medium btn-ghost hover:text-red-400 border hover:bg-red-500/10 transition-all flex items-center gap-1.5"
              >
                <IconTrash size={14} />
                Delete
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Mantine Modal for Emails */}
      <Modal
        opened={emailModalOpened}
        onClose={() => { setEmailModalOpened(false); setSelectedEmail(null); }}
        title="Emails"
        size="lg"
        centered
        lockScroll={false}
      >
        <div className="space-y-4">
          {mockEmails.length === 0 ? (
            <Text c="dimmed" ta="center" py="xl">No emails found</Text>
          ) : (
            mockEmails.map((email) => (
              <Paper
                key={email.id}
                p="md"
                withBorder
                style={{ backgroundColor: selectedEmail?.id === email.id ? '#25262b' : 'transparent', borderColor: selectedEmail?.id === email.id ? '#228be6' : '#373a40', cursor: 'pointer' }}
                onClick={() => setSelectedEmail(selectedEmail?.id === email.id ? null : email)}
              >
                <Group justify="space-between" mb="xs">
                  <div>
                    <Text fw={500} c="white">{email.subject}</Text>
                    <Text size="xs" c="dimmed">From: {email.from}</Text>
                  </div>
                  <Text size="xs" c="dimmed">
                    {email.date.toLocaleDateString()}
                  </Text>
                </Group>
                
                {selectedEmail?.id === email.id && (
                  <>
                    <Divider my="sm" color="dark.4" />
                    <Text size="sm" c="gray.3">{email.body}</Text>
                  </>
                )}
              </Paper>
            ))
          )}
        </div>
      </Modal>
    </>
  );
}
