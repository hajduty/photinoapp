'use client';

import React, { useState } from 'react';
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
import { Tag } from '../../types/tag/tag';
import { getContrastColor } from '../../utils/getContrastColor';
import { useTags, useCreateTag, useUpdateTag, useDeleteTag } from '../../hooks/useTags';

interface TagManagementProps {
  className?: string;
}

const inputCls = {
  input: 'bg-neutral-800 border-neutral-700 text-neutral-200 placeholder-neutral-500',
  error: 'text-red-400',
};

export default function TagManagement({ className }: TagManagementProps) {
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [deleteModalOpen, setDeleteModalOpen] = useState(false);

  const [newTagName, setNewTagName] = useState('');
  const [newTagColor, setNewTagColor] = useState('#3b82f6');
  const [editTagName, setEditTagName] = useState('');
  const [editTagColor, setEditTagColor] = useState('#3b82f6');
  const [tagToDelete, setTagToDelete] = useState<Tag | null>(null);
  const [editingTag, setEditingTag] = useState<Tag | null>(null);

  const [nameError, setNameError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const { data: tags = [], isLoading, error } = useTags();
  const createTagMutation = useCreateTag();
  const updateTagMutation = useUpdateTag();
  const deleteTagMutation = useDeleteTag();

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
      await createTagMutation.mutateAsync({ Name: newTagName.trim(), Color: newTagColor });
      setCreateModalOpen(false);
      setNewTagName('');
      setNewTagColor('#3b82f6');
      setNameError('');
    } catch (err) {
      console.error('Failed to create tag:', err);
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
      await updateTagMutation.mutateAsync({ TagId: editingTag.Id, NewName: editTagName.trim(), NewColor: editTagColor });
      setEditModalOpen(false);
      setEditingTag(null);
      setEditTagName('');
      setEditTagColor('#3b82f6');
      setNameError('');
    } catch (err) {
      console.error('Failed to update tag:', err);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteTag = async () => {
    if (!tagToDelete) return;
    try {
      setIsSubmitting(true);
      await deleteTagMutation.mutateAsync({ TagId: tagToDelete.Id });
      setDeleteModalOpen(false);
      setTagToDelete(null);
    } catch (err) {
      console.error('Failed to delete tag:', err);
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
        <Badge
          color="gray"
          variant="light"
          style={{ backgroundColor: tag.Color, color: getContrastColor(tag.Color), border: 'none' }}
        >
          {tag.Name}
        </Badge>
      </Table.Td>
      <Table.Td>
        <Text size="sm" c="dimmed">{tag.Color}</Text>
      </Table.Td>
      <Table.Td>
        <Group gap={4} justify="flex-end">
          <ActionIcon variant="subtle" color="gray" onClick={() => openEditModal(tag)} aria-label="Edit tag">
            <IconEdit style={{ width: rem(16), height: rem(16) }} />
          </ActionIcon>
          <ActionIcon variant="subtle" color="gray" onClick={() => openDeleteModal(tag)} aria-label="Delete tag">
            <IconTrash style={{ width: rem(16), height: rem(16) }} />
          </ActionIcon>
        </Group>
      </Table.Td>
    </Table.Tr>
  ));

  return (
    <div className={className}>
      <div className="mb-4 flex items-center justify-between">
        <p className="text-sm font-semibold text-neutral-300">Tag Management</p>
        <button
          onClick={() => setCreateModalOpen(true)}
          className="btn-secondary text-sm flex items-center gap-2"
        >
          <IconPlus size={15} />
          New Tag
        </button>
      </div>

      {error && (
        <div className="rounded-xl border border-neutral-800 bg-neutral-900/50 px-4 py-3 flex items-center gap-3 mb-3">
          <div className="w-7 h-7 bg-red-500/20 rounded-full flex items-center justify-center flex-shrink-0">
            <IconAlertCircle className="w-4 h-4 text-red-400" />
          </div>
          <p className="text-sm text-red-400">{error.message}</p>
        </div>
      )}

      {isLoading ? (
        <div className="flex items-center justify-center py-8">
          <Loader />
        </div>
      ) : tags.length === 0 ? (
        <div className="rounded-xl border border-neutral-800 bg-neutral-900/50 p-8 text-center">
          <div className="w-12 h-12 bg-neutral-800 rounded-full flex items-center justify-center mx-auto mb-3">
            <IconColorPicker className="w-6 h-6 text-neutral-500" />
          </div>
          <p className="text-sm text-neutral-400">No tags yet. Create one to start organizing your job postings.</p>
        </div>
      ) : (
        <div className="rounded-xl overflow-hidden border border-neutral-800">
          <Table highlightOnHover className="bg-neutral-900">
            <Table.Thead>
              <Table.Tr>
                <Table.Th className="text-neutral-500 text-xs font-semibold uppercase tracking-widest">Tag</Table.Th>
                <Table.Th className="text-neutral-500 text-xs font-semibold uppercase tracking-widest">Color</Table.Th>
                <Table.Th style={{ width: 80 }} />
              </Table.Tr>
            </Table.Thead>
            <Table.Tbody>{rows}</Table.Tbody>
          </Table>
        </div>
      )}

      {/* Create Tag Modal */}
      <Modal
        lockScroll={false}
        opened={createModalOpen}
        onClose={() => { setCreateModalOpen(false); setNewTagName(''); setNewTagColor('#3b82f6'); setNameError(''); }}
        title="Create New Tag"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200 text-sm font-semibold',
          close: 'text-neutral-400 hover:text-white',
        }}
      >
        <div className="mb-4">
          <p className="text-xs text-neutral-500 mb-1.5">Tag Name</p>
          <TextInput
            placeholder="Enter tag name"
            value={newTagName}
            onChange={(event) => { setNewTagName(event.currentTarget.value); if (nameError) setNameError(validateName(event.currentTarget.value)); }}
            error={nameError}
            classNames={inputCls}
          />
        </div>
        <div className="mb-4">
          <p className="text-neutral-400 text-xs font-medium uppercase tracking-wide mb-2">Tag Color</p>
          <ColorPicker value={newTagColor} onChange={setNewTagColor} format="hex" withPicker fullWidth
            swatches={['#ef4444', '#f97316', '#eab308', '#22c55e', '#3b82f6', '#6366f1', '#a855f7', '#ec4899', '#6b7280', '#06b6d4', '#84cc16', '#f43f5e', '#a78bfa', '#f59e0b']}
          />
        </div>
        <Group justify="flex-end" mt="md">
          <button onClick={() => setCreateModalOpen(false)} disabled={isSubmitting} className="btn-ghost text-sm">Cancel</button>
          <button onClick={handleCreateTag} disabled={isSubmitting} className="btn-secondary text-sm flex items-center gap-2">
            <IconCheck size={15} /> Create Tag
          </button>
        </Group>
      </Modal>

      {/* Edit Tag Modal */}
      <Modal
        lockScroll={false}
        opened={editModalOpen}
        onClose={() => { setEditModalOpen(false); setEditingTag(null); setEditTagName(''); setEditTagColor('#3b82f6'); setNameError(''); }}
        title="Edit Tag"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200 text-sm font-semibold',
          close: 'text-neutral-400 hover:text-white',
        }}
      >
        <div className="mb-4">
          <p className="text-xs text-neutral-500 mb-1.5">Tag Name</p>
          <TextInput
            placeholder="Enter tag name"
            value={editTagName}
            onChange={(event) => { setEditTagName(event.currentTarget.value); if (nameError) setNameError(validateName(event.currentTarget.value)); }}
            error={nameError}
            classNames={inputCls}
          />
        </div>
        <div className="mb-4">
          <p className="text-neutral-400 text-xs font-medium uppercase tracking-wide mb-2">Tag Color</p>
          <ColorPicker value={editTagColor} onChange={setEditTagColor} format="hex" withPicker fullWidth
            swatches={['#ef4444', '#f97316', '#eab308', '#22c55e', '#3b82f6', '#6366f1', '#a855f7', '#ec4899', '#6b7280', '#06b6d4', '#84cc16', '#f43f5e', '#a78bfa', '#f59e0b']}
          />
        </div>
        <Group justify="flex-end" mt="md">
          <button onClick={() => setEditModalOpen(false)} disabled={isSubmitting} className="btn-ghost text-sm">Cancel</button>
          <button onClick={handleUpdateTag} disabled={isSubmitting} className="btn-secondary text-sm flex items-center gap-2">
            <IconCheck size={15} /> Update Tag
          </button>
        </Group>
      </Modal>

      {/* Delete Tag Modal */}
      <Modal
        opened={deleteModalOpen}
        onClose={() => { setDeleteModalOpen(false); setTagToDelete(null); }}
        title="Delete Tag"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200 text-sm font-semibold',
          close: 'text-neutral-400 hover:text-white',
        }}
      >
        {tagToDelete && (
          <div>
            <Text size="sm" className="text-neutral-200 mb-2">
              Are you sure you want to delete <strong>{tagToDelete.Name}</strong>?
            </Text>
            <Text size="sm" className="text-neutral-500 mb-6">This action cannot be undone.</Text>
            <Group justify="flex-end">
              <button onClick={() => setDeleteModalOpen(false)} disabled={isSubmitting} className="btn-ghost text-sm">Cancel</button>
              <button onClick={handleDeleteTag} disabled={isSubmitting} className="btn-secondary text-sm flex items-center gap-2">
                <IconTrash size={15} /> Delete
              </button>
            </Group>
          </div>
        )}
      </Modal>
    </div>
  );
}
