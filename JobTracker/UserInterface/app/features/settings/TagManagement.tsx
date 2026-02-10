'use client';

import React, { useState, useEffect } from 'react';
import {
  TextInput,
  Table,
  ActionIcon,
  Group,
  Modal,
  Text,
  Badge,
  rem,
  Loader,
  ColorPicker
} from '@mantine/core';
import {
  IconPlus,
  IconEdit,
  IconTrash,
  IconColorPicker,
  IconAlertCircle,
  IconCheck,
  IconX
} from '@tabler/icons-react';
import { sendPhotinoRequest } from '../../utils/photino';
import { Tag } from '../../types/tag/tag';
import { CreateTagRequest } from '../../types/tag/create-tag-request';
import { CreateTagResponse } from '../../types/tag/create-tag-response';
import { UpdateTagRequest } from '../../types/tag/update-tag-request';
import { UpdateTagResponse } from '../../types/tag/update-tag-response';
import { DeleteTagRequest } from '../../types/tag/delete-tag-request';
import { DeleteTagResponse } from '../../types/tag/delete-tag-response';
import { getContrastColor } from '../../utils/getContrastColor';

interface TagManagementProps {
  className?: string;
}

export default function TagManagement({ className }: TagManagementProps) {
  const [tags, setTags] = useState<Tag[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Modal states
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [deleteModalOpen, setDeleteModalOpen] = useState(false);

  // Form states
  const [newTagName, setNewTagName] = useState('');
  const [newTagColor, setNewTagColor] = useState('#3b82f6');
  const [editTagName, setEditTagName] = useState('');
  const [editTagColor, setEditTagColor] = useState('#3b82f6');
  const [tagToDelete, setTagToDelete] = useState<Tag | null>(null);
  const [editingTag, setEditingTag] = useState<Tag | null>(null);

  // Form validation
  const [nameError, setNameError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    fetchTags();
  }, []);

  const fetchTags = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await sendPhotinoRequest<any>('tags.getTags', { test: "anything" });
      console.log(response);
      setTags(response);
    } catch (err) {
      console.error('Failed to fetch tags:', err);
      setError('Failed to load tags. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const validateName = (name: string): string => {
    if (!name.trim()) return 'Tag name is required';
    if (name.length < 2) return 'Tag name must be at least 2 characters';
    if (name.length > 50) return 'Tag name cannot exceed 50 characters';
    if (tags.some(tag => tag.Name.toLowerCase() === name.toLowerCase() && tag.Id !== editingTag?.Id)) {
      return 'A tag with this name already exists';
    }
    return '';
  };

  const handleCreateTag = async () => {
    const nameValidationError = validateName(newTagName);
    setNameError(nameValidationError);

    if (nameValidationError) return;

    try {
      setIsSubmitting(true);
      const request: CreateTagRequest = {
        Name: newTagName.trim(),
        Color: newTagColor
      };


      const response = await sendPhotinoRequest<CreateTagResponse>('tags.createTag', request);
      console.log(response);
      setTags(prev => [...prev, response.CreatedTag]);
      setCreateModalOpen(false);
      setNewTagName('');
      setNewTagColor('#3b82f6');
      setNameError('');
    } catch (err) {
      console.error('Failed to create tag:', err);
      setError('Failed to create tag. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleUpdateTag = async () => {
    if (!editingTag) return;

    const nameValidationError = validateName(editTagName);
    setNameError(nameValidationError);

    if (nameValidationError) return;

    try {
      setIsSubmitting(true);
      const request: UpdateTagRequest = {
        TagId: editingTag.Id,
        NewName: editTagName.trim(),
        NewColor: editTagColor
      };

      const response = await sendPhotinoRequest<UpdateTagResponse>('tags.updateTag', request);
      setTags(prev => prev.map(tag =>
        tag.Id === editingTag.Id ? response.UpdatedTag : tag
      ));
      setEditModalOpen(false);
      setEditingTag(null);
      setEditTagName('');
      setEditTagColor('#3b82f6');
      setNameError('');
    } catch (err) {
      console.error('Failed to update tag:', err);
      setError('Failed to update tag. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteTag = async () => {
    if (!tagToDelete) return;

    try {
      setIsSubmitting(true);
      const request: DeleteTagRequest = {
        TagId: tagToDelete.Id
      };

      await sendPhotinoRequest<DeleteTagResponse>('tags.deleteTag', request);
      setTags(prev => prev.filter(tag => tag.Id !== tagToDelete.Id));
      setDeleteModalOpen(false);
      setTagToDelete(null);
    } catch (err) {
      console.error('Failed to delete tag:', err);
      setError('Failed to delete tag. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const openEditModal = (tag: Tag) => {
    setEditingTag(tag);
    setEditTagName(tag.Name);
    setEditTagColor(tag.Color);
    setEditModalOpen(true);
  };

  const openDeleteModal = (tag: Tag) => {
    setTagToDelete(tag);
    setDeleteModalOpen(true);
  };

  const rows = tags.map((tag) => (
    <Table.Tr key={tag.Id}>
      <Table.Td>
        <Group gap="sm">
          <Badge
            color="gray"
            variant="light"
            style={{
              backgroundColor: tag.Color,
              color: getContrastColor(tag.Color),
              border: 'none'
            }}
          >
            {tag.Name}
          </Badge>
        </Group>
      </Table.Td>
      <Table.Td>
        <Text size="sm" c="dimmed">{tag.Color}</Text>
      </Table.Td>
      <Table.Td>
        <Group gap={4} justify="flex-end">
          <ActionIcon
            variant="subtle"
            color="gray"
            onClick={() => openEditModal(tag)}
            aria-label="Edit tag"
          >
            <IconEdit style={{ width: rem(16), height: rem(16) }} />
          </ActionIcon>
          <ActionIcon
            variant="subtle"
            color="gray"
            onClick={() => openDeleteModal(tag)}
            aria-label="Delete tag"
          >
            <IconTrash style={{ width: rem(16), height: rem(16) }} />
          </ActionIcon>
        </Group>
      </Table.Td>
    </Table.Tr>
  ));

  return (
    <div className={className}>
      {/* Header and Create Button */}
      <div className="py-6">
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-lg font-bold text-neutral-200 mb-2">TAG MANAGEMENT</h1>
            <p className="text-neutral-400">Create and manage tags for organizing your job postings</p>
          </div>
          <button 
            onClick={() => setCreateModalOpen(true)}
            className="btn-secondary text-sm flex items-center gap-2"
          >
            <IconPlus size={16} />
            Create New Tag
          </button>
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

      {/* Loading State */}
      {loading ? (
        <div className="flex items-center justify-center py-8">
          <Loader />
        </div>
      ) : (
        <div>
          {tags.length === 0 ? (
            <div className="card p-8 text-center">
              <div className="w-16 h-16 bg-neutral-800 rounded-full flex items-center justify-center mx-auto mb-4">
                <IconColorPicker className="w-8 h-8 text-neutral-400" />
              </div>
              <h3 className="text-white text-lg font-semibold mb-2">No Tags Found</h3>
              <p className="text-neutral-400">
                Create your first tag to get started organizing your job postings.
              </p>
            </div>
          ) : (
            <Table highlightOnHover className="bg-neutral-900 border border-neutral-800">
              <Table.Thead>
                <Table.Tr>
                  <Table.Th className="text-neutral-200 font-semibold">Tag</Table.Th>
                  <Table.Th className="text-neutral-200 font-semibold">Color Code</Table.Th>
                  <Table.Th style={{ width: 120 }} className="text-neutral-200 font-semibold">Actions</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>{rows}</Table.Tbody>
            </Table>
          )}
        </div>
      )}

      {/* Create Tag Modal */}
      <Modal
        opened={createModalOpen}
        onClose={() => {
          setCreateModalOpen(false);
          setNewTagName('');
          setNewTagColor('#3b82f6');
          setNameError('');
        }}
        title="Create New Tag"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200',
          close: 'text-neutral-400 hover:text-white'
        }}
      >
        <TextInput
          label="Tag Name"
          placeholder="Enter tag name"
          value={newTagName}
          onChange={(event) => {
            setNewTagName(event.currentTarget.value);
            if (nameError) setNameError(validateName(event.currentTarget.value));
          }}
          error={nameError}
          className="mb-4"
          classNames={{
            input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
            label: 'text-neutral-300',
            error: 'text-red-400'
          }}
        />
        <div className="mb-4">
          <Text className="text-neutral-300 mb-2">Tag Color</Text>
          <ColorPicker
            value={newTagColor}
            onChange={setNewTagColor}
            format="hex"
            withPicker={true}
            fullWidth
            swatches={[
              '#ef4444', '#f97316', '#eab308', '#22c55e', '#3b82f6', '#6366f1', '#a855f7', '#ec4899',
              '#6b7280', '#111827', '#06b6d4', '#84cc16', '#f43f5e', '#a78bfa', '#f59e0b'
            ]}
          />
        </div>
        <Group justify="flex-end" mt="md">
          <button 
            onClick={() => setCreateModalOpen(false)}
            disabled={isSubmitting}
            className="btn-ghost text-sm"
          >
            Cancel
          </button>
          <button 
            onClick={handleCreateTag}
            disabled={isSubmitting}
            className="btn-secondary text-sm flex items-center gap-2"
          >
            <IconCheck size={16} />
            Create Tag
          </button>
        </Group>
      </Modal>

      {/* Edit Tag Modal */}
      <Modal
        opened={editModalOpen}
        onClose={() => {
          setEditModalOpen(false);
          setEditingTag(null);
          setEditTagName('');
          setEditTagColor('#3b82f6');
          setNameError('');
        }}
        title="Edit Tag"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200',
          close: 'text-neutral-400 hover:text-white'
        }}
      >
        <TextInput
          label="Tag Name"
          placeholder="Enter tag name"
          value={editTagName}
          onChange={(event) => {
            setEditTagName(event.currentTarget.value);
            if (nameError) setNameError(validateName(event.currentTarget.value));
          }}
          error={nameError}
          className="mb-4"
          classNames={{
            input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
            label: 'text-neutral-300',
            error: 'text-red-400'
          }}
        />
        <div className="mb-4">
          <Text className="text-neutral-300 mb-2">Tag Color</Text>
          <ColorPicker
            value={editTagColor}
            onChange={setEditTagColor}
            format="hex"
            withPicker={true}
            fullWidth
            swatches={[
              '#ef4444', '#f97316', '#eab308', '#22c55e', '#3b82f6', '#6366f1', '#a855f7', '#ec4899',
              '#6b7280', '#111827', '#06b6d4', '#84cc16', '#f43f5e', '#a78bfa', '#f59e0b'
            ]}
          />
        </div>
        <Group justify="flex-end" mt="md">
          <button 
            onClick={() => setEditModalOpen(false)}
            disabled={isSubmitting}
            className="btn-ghost text-sm"
          >
            Cancel
          </button>
          <button 
            onClick={handleUpdateTag}
            disabled={isSubmitting}
            className="btn-secondary text-sm flex items-center gap-2"
          >
            <IconCheck size={16} />
            Update Tag
          </button>
        </Group>
      </Modal>

      {/* Delete Tag Modal */}
      <Modal
        opened={deleteModalOpen}
        onClose={() => {
          setDeleteModalOpen(false);
          setTagToDelete(null);
        }}
        title="Delete Tag"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200',
          close: 'text-neutral-400 hover:text-white'
        }}
      >
        {tagToDelete && (
          <div>
            <Text className="text-neutral-200 mb-4">
              Are you sure you want to delete the tag "<strong>{tagToDelete.Name}</strong>"?
            </Text>
            <Text size="sm" className="text-neutral-400 mb-6">
              This action cannot be undone.
            </Text>
            <Group justify="flex-end">
              <button 
                onClick={() => setDeleteModalOpen(false)}
                disabled={isSubmitting}
                className="btn-ghost text-sm flex items-center gap-2"
              >
                <IconX size={16} />
                Cancel
              </button>
              <button 
                onClick={handleDeleteTag}
                disabled={isSubmitting}
                className="btn-secondary text-sm flex items-center gap-2"
              >
                <IconTrash size={16} />
                Delete
              </button>
            </Group>
          </div>
        )}
      </Modal>
    </div>
  );
}