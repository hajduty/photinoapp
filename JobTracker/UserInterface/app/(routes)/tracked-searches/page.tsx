'use client';

import React, { useState, useEffect } from 'react';
import {
  Table,
  ActionIcon,
  Group,
  Modal,
  Switch,
  TextInput,
  NumberInput,
  Loader,
  Select,
  MultiSelect
} from '@mantine/core';
import {
  IconPlus,
  IconEdit,
  IconTrash,
  IconBell,
  IconCheck,
  IconRefresh,
  IconX
} from '@tabler/icons-react';
import { sendPhotinoRequest } from '../../utils/photino';
import { JobTracker } from '../../types/jobs/job-tracker';
import { Tag } from '../../types/tag/tag';
import { CreateJobTrackerRequest } from '../../types/jobs/create-job-tracker-request';
import { CreateJobTrackerResponse } from '../../types/jobs/create-job-tracker-response';
import { UpdateJobTrackerRequest } from '../../types/jobs/update-job-tracker-request';
import { DeleteJobTrackerRequest } from '../../types/tag/delete-job-tracker-request';
import { DeleteJobTrackerResponse } from '../../types/tag/delete-job-tracker-response';
import { getContrastColor } from '../../utils/getContrastColor';
import { notifications } from '@mantine/notifications';

const SOURCES = ['All', 'LinkedIn', 'Arbetsförmedlingen', 'Indeed'];

export default function TrackersPage() {
  const [trackers, setTrackers] = useState<JobTracker[]>([]);
  const [loading, setLoading] = useState(true);
  const [availableTags, setAvailableTags] = useState<Tag[]>([]);

  const [modalOpen, setModalOpen] = useState(false);
  const [editingTracker, setEditingTracker] = useState<JobTracker | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const [formData, setFormData] = useState<CreateJobTrackerRequest>({
    Keyword: '',
    Source: 'All',
    Location: 'All',
    IsActive: true,
    Tags: [],
    LastCheckedAt: new Date(),
    CheckIntervalHours: 1
  });

  useEffect(() => {
    fetchTrackers();
    fetchTags();
  }, []);

  const fetchTrackers = async () => {
    try {
      setLoading(true);
      const response = await sendPhotinoRequest<JobTracker[]>('jobTracker.getTrackers', {});
      setTrackers(response);
      console.log(response);
    } catch (err) {
      console.error('Failed to fetch trackers:', err);
    } finally {
      setLoading(false);
    }
  };

  const fetchTags = async () => {
    try {
      const response = await sendPhotinoRequest<Tag[]>('tags.getTags', {});
      setAvailableTags(response);
    } catch (err) {
      console.error('Failed to fetch tags:', err);
    }
  };

  const handleCreate = async () => {
    if (!formData.Keyword.trim()) return;

    try {
      setIsSubmitting(true);
      await sendPhotinoRequest<CreateJobTrackerResponse>('jobTracker.createTracker', formData);
      setModalOpen(false);
      resetForm();
      fetchTrackers();
    } catch (err) {
      console.error('Failed to create tracker:', err);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleUpdate = async () => {
    if (!editingTracker || !formData.Keyword.trim()) return;

    try {
      setIsSubmitting(true);
      const request: UpdateJobTrackerRequest = {
        TrackerId: editingTracker.Id,
        Keyword: formData.Keyword,
        Source: formData.Source,
        Location: formData.Location,
        IsActive: formData.IsActive,
        Tags: formData.Tags,
        LastCheckedAt: editingTracker.LastCheckedAt,
        CheckIntervalHours: formData.CheckIntervalHours
      };
      await sendPhotinoRequest('jobTracker.updateTracker', request);
      setModalOpen(false);
      resetForm();
      fetchTrackers();
    } catch (err) {
      console.error('Failed to update tracker:', err);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (tracker: JobTracker) => {
    try {
      const request: DeleteJobTrackerRequest = {
        TrackerId: tracker.Id
      };
      await sendPhotinoRequest<DeleteJobTrackerResponse>('jobTracker.deleteTracker', request);
      fetchTrackers();
    } catch (err) {
      console.error('Failed to delete tracker:', err);
    }
  };

  const handleToggle = async (tracker: JobTracker) => {
    // Optimistic update: update local state first so animation plays
    setTrackers(trackers.map(t => 
      t.Id === tracker.Id ? { ...t, IsActive: !t.IsActive } : t
    ));

    try {
      const request: UpdateJobTrackerRequest = {
        TrackerId: tracker.Id,
        Keyword: tracker.Keyword,
        Source: tracker.Source,
        Location: tracker.Location,
        IsActive: !tracker.IsActive,
        Tags: tracker.Tags,
        LastCheckedAt: tracker.LastCheckedAt,
        CheckIntervalHours: tracker.CheckIntervalHours
      };
      await sendPhotinoRequest('jobTracker.updateTracker', request);
    } catch (err) {
      // Revert on error
      console.error('Failed to toggle tracker:', err);
      setTrackers(trackers.map(t => 
        t.Id === tracker.Id ? { ...t, IsActive: tracker.IsActive } : t
      ));
    }
  };

  const handleSync = async () => {
    try {
      const response = await sendPhotinoRequest('jobTracker.process', { Data: "" });

      notifications.show({
        title: 'Synced jobs',
        message: "Added " + response.JobsAdded + " new jobs",
        color: 'green',
        icon: <IconCheck size={16} />
      });
      fetchTrackers();
    } catch (err) {
      console.error('Failed to sync jobs:', err);
      notifications.show({
        title: 'Failed to sync jobs',
        message: 'Could not sync jobs at this time',
        color: 'red',
        icon: <IconX size={16} />
      });
    }
  };

  const openModal = (tracker?: JobTracker) => {
    if (tracker) {
      setEditingTracker(tracker);
      setFormData({
        Keyword: tracker.Keyword,
        Source: tracker.Source,
        Location: tracker.Location,
        IsActive: tracker.IsActive,
        Tags: tracker.Tags,
        LastCheckedAt: tracker.LastCheckedAt,
        CheckIntervalHours: tracker.CheckIntervalHours
      });
    } else {
      setEditingTracker(null);
      resetForm();
    }
    setModalOpen(true);
  };

  const resetForm = () => {
    setFormData({
      Keyword: '',
      Source: 'All',
      Location: 'All',
      IsActive: true,
      Tags: [],
      LastCheckedAt: new Date(),
      CheckIntervalHours: 1
    });
  };

  const formatDate = (date: Date | null) => {
    if (!date) return 'Never';
    return new Date(date).toLocaleString('sv-SE', {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  return (
    <div className="p-4 md:p-8">
      <div className="max-w-7xl mx-auto space-y-8">
        <div className="flex items-start justify-between mb-6">
          <div>
            <h1 className="text-2xl font-bold text-neutral-200 mb-2">JOB TRACKERS</h1>
            <p className="text-neutral-400">Save keywords and locations, we'll automatically pull new jobs for you in the background.</p>
          </div>
          <button 
            onClick={() => openModal()} 
            className="btn-secondary text-sm flex items-center"
          >
            <IconPlus size={16} />
            Add Tracker
          </button>
        </div>

        {loading ? (
          <div className="flex justify-center py-8">
            <Loader />
          </div>
        ) : trackers.length === 0 ? (
          <div className="card p-8 text-center">
            <IconBell className="w-12 h-12 text-neutral-500 mx-auto mb-4" />
            <p className="text-neutral-400">No trackers yet. Create one to get started.</p>
          </div>
        ) : (
          <Table highlightOnHover className="bg-neutral-900 border border-neutral-800">
            <Table.Thead>
              <Table.Tr>
                <Table.Th className="text-neutral-200">Keyword</Table.Th>
                <Table.Th className="text-neutral-200">Source</Table.Th>
                <Table.Th className="text-neutral-200">Location</Table.Th>
                <Table.Th className="text-neutral-200">Check Every</Table.Th>
                <Table.Th className="text-neutral-200 flex items-center gap-2">
                  Last Checked
                  <ActionIcon variant="subtle" color="gray" size="sm" onClick={() => handleSync()}>
                    <IconRefresh size={14} />
                  </ActionIcon>
                </Table.Th>
                <Table.Th className="text-neutral-200">Tags</Table.Th>
                <Table.Th className="text-neutral-200">Active</Table.Th>
                <Table.Th style={{ width: 100 }} />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>
              {trackers.map((tracker) => (
                <Table.Tr key={tracker.Id}>
                  <Table.Td className="text-neutral-200">{tracker.Keyword}</Table.Td>
                  <Table.Td className="text-neutral-400">{tracker.Source}</Table.Td>
                  <Table.Td className="text-neutral-400">{tracker.Location}</Table.Td>
                  <Table.Td className="text-neutral-400">{tracker.CheckIntervalHours}h</Table.Td>
                  <Table.Td className="text-neutral-400">{formatDate(tracker.LastCheckedAt)}</Table.Td>
                  <Table.Td>
                    <Group gap={4}>
                      {tracker.Tags?.map((tag: Tag) => (
                        <span
                          key={tag.Id}
                          className="px-2 py-0.5 rounded text-xs"
                          style={{ backgroundColor: tag.Color, color: getContrastColor(tag.Color) }}
                        >
                          {tag.Name}
                        </span>
                      ))}
                    </Group>
                  </Table.Td>
                  <Table.Td>
                    <Switch
                      checked={tracker.IsActive}
                      onChange={() => handleToggle(tracker)}
                      size="sm"
                    />
                  </Table.Td>
                  <Table.Td>
                    <Group gap={4} justify="flex-end">
                      <ActionIcon variant="subtle" color="gray" onClick={() => openModal(tracker)}>
                        <IconEdit size={16} />
                      </ActionIcon>
                      <ActionIcon variant="subtle" color="gray" onClick={() => handleDelete(tracker)}>
                        <IconTrash size={16} />
                      </ActionIcon>
                    </Group>
                  </Table.Td>
                </Table.Tr>
              ))}
            </Table.Tbody>
          </Table>
        )}

        <Modal
          opened={modalOpen}
          onClose={() => setModalOpen(false)}
          title={editingTracker ? 'Edit Tracker' : 'New Tracker'}
          centered
          lockScroll={false}
          classNames={{
            content: 'bg-neutral-900 border border-neutral-800',
            title: 'text-neutral-200',
            close: 'text-neutral-400 hover:text-white'
          }}
        >
          <TextInput
            label="Keyword"
            placeholder="e.g. software developer"
            value={formData.Keyword}
            onChange={(e) => setFormData({ ...formData, Keyword: e.currentTarget.value })}
            className="mb-4"
            classNames={{
              input: 'bg-neutral-800 border-neutral-700 text-neutral-200',
              label: 'text-neutral-300'
            }}
          />

          <Select
            label="Source"
            value={formData.Source}
            onChange={(v) => setFormData({ ...formData, Source: v || 'All' })}
            data={SOURCES}
            className="mb-4"
            classNames={{
              input: 'bg-neutral-800 border-neutral-700 text-neutral-200',
              label: 'text-neutral-300',
              dropdown: 'bg-neutral-800 border-neutral-700',
              option: 'text-neutral-200 hover:bg-neutral-700'
            }}
          />

          <TextInput
            label="Location"
            value={formData.Location}
            onChange={(e) => setFormData({ ...formData, Location: e.currentTarget.value })}
            className="mb-4"
            classNames={{
              input: 'bg-neutral-800 border-neutral-700 text-neutral-200',
              label: 'text-neutral-300'
            }}
          />

          <NumberInput
            label="Check Interval (hours)"
            value={formData.CheckIntervalHours}
            onChange={(v) => setFormData({ ...formData, CheckIntervalHours: Number(v) || 1 })}
            min={1}
            max={168}
            className="mb-4"
            classNames={{
              input: 'bg-neutral-800 border-neutral-700 text-neutral-200',
              label: 'text-neutral-300'
            }}
          />

          <MultiSelect
            label="Tags to notify about"
            placeholder="Select tags"
            value={formData.Tags.map(t => t.Id.toString())}
            onChange={(selectedIds) => {
              const selectedTags = availableTags.filter(t => selectedIds.includes(t.Id.toString()));
              setFormData({ ...formData, Tags: selectedTags });
            }}
            data={availableTags.map(t => ({ value: t.Id.toString(), label: t.Name }))}
            className="mb-6"
            classNames={{
              input: 'bg-neutral-800 border-neutral-700 text-neutral-200',
              label: 'text-neutral-300',
              dropdown: 'bg-neutral-800 border-neutral-700',
              option: 'text-neutral-200 hover:bg-neutral-700'
            }}
          />

          <Group justify="flex-end">
            <button 
              onClick={() => setModalOpen(false)} 
              disabled={isSubmitting} 
              className="btn-ghost text-sm"
            >
              Cancel
            </button>
            <button 
              onClick={editingTracker ? handleUpdate : handleCreate} 
              disabled={isSubmitting} 
              className="btn-secondary text-sm flex items-center gap-2"
            >
              <IconCheck size={16} />
              Save
            </button>
          </Group>
        </Modal>
      </div>
    </div>
  );
}
