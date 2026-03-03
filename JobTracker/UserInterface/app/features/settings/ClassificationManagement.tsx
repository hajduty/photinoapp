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
  IconAlertCircle,
  IconCheck,
  IconX
} from '@tabler/icons-react';
import { sendPhotinoRequest } from '../../utils/photino';
import { Classification } from '../../types/classifications/classification';
import { Prototype } from '../../types/prototypes/prototype';
import { CreateClassificationRequest } from '../../types/classifications/create-classification-request';
import { CreateClassificationResponse } from '../../types/classifications/create-classification-response';
import { DeleteClassificationRequest } from '../../types/classifications/delete-classification-request';
import { DeleteClassificationResponse } from '../../types/classifications/delete-classification-response';
import { getContrastColor } from '../../utils/getContrastColor';
import ClassificationPrototypeModal from './ClassificationPrototypeModal';

interface ClassificationManagementProps {
  className?: string;
}

export default function ClassificationManagement({ className }: ClassificationManagementProps) {
  const [classifications, setClassifications] = useState<Classification[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Modal states
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [deleteModalOpen, setDeleteModalOpen] = useState(false);
  const [prototypeModalOpen, setPrototypeModalOpen] = useState(false);

  // Form states
  const [newClassificationName, setNewClassificationName] = useState('');
  const [newClassificationColor, setNewClassificationColor] = useState<string | undefined>(undefined);
  const [classificationToDelete, setClassificationToDelete] = useState<Classification | null>(null);
  const [classificationToEdit, setClassificationToEdit] = useState<Classification | null>(null);

  // Form validation
  const [nameError, setNameError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    fetchClassifications();
  }, []);

  const fetchClassifications = async () => {
    try {
      setLoading(true);
      setError(null);
      const response = await sendPhotinoRequest<any>('classifications.get', {});
      setClassifications(response.Classifications || []);
    } catch (err) {
      console.error('Failed to fetch classifications:', err);
      setError('Failed to load classifications. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const validateName = (name: string): string => {
    if (!name.trim()) return 'Classification name is required';
    if (name.length < 2) return 'Classification name must be at least 2 characters';
    if (name.length > 50) return 'Classification name cannot exceed 50 characters';
    if (classifications.some(cls => cls.Name.toLowerCase() === name.toLowerCase() && cls.Id !== classificationToEdit?.Id)) {
      return 'A classification with this name already exists';
    }
    return '';
  };

  const handleCreateClassification = async () => {
    const nameValidationError = validateName(newClassificationName);
    setNameError(nameValidationError);

    if (nameValidationError) return;

    try {
      setIsSubmitting(true);
      const request: CreateClassificationRequest = {
        Name: newClassificationName.trim(),
        ...(newClassificationColor && { Color: newClassificationColor })
      };

      const response = await sendPhotinoRequest<CreateClassificationResponse>('classifications.create', request);
      setClassifications(prev => [...prev, response.Classification]);
      setCreateModalOpen(false);
      setNewClassificationName('');
      setNameError('');
    } catch (err) {
      console.error('Failed to create classification:', err);
      setError('Failed to create classification. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeleteClassification = async () => {
    if (!classificationToDelete) return;

    try {
      setIsSubmitting(true);
      const request: DeleteClassificationRequest = {
        ClassificationId: classificationToDelete.Id
      };

      await sendPhotinoRequest<DeleteClassificationResponse>('classifications.delete', request);
      setClassifications(prev => prev.filter(cls => cls.Id !== classificationToDelete.Id));
      setDeleteModalOpen(false);
      setClassificationToDelete(null);
    } catch (err) {
      console.error('Failed to delete classification:', err);
      setError('Failed to delete classification. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const openEditModal = (classification: Classification) => {
    setClassificationToEdit(classification);
    setEditModalOpen(true);
  };

  const openDeleteModal = (classification: Classification) => {
    setClassificationToDelete(classification);
    setDeleteModalOpen(true);
  };


  const rows = classifications.map((classification) => (
    <Table.Tr key={classification.Id}>
      <Table.Td>
        <Group gap="sm">
          <Badge
            color="gray"
            variant="light"
            style={{
              backgroundColor: classification.Color || 'transparent',
              color: classification.Color ? getContrastColor(classification.Color) : 'inherit',
              border: 'none'
            }}
          >
            {classification.Name}
          </Badge>
        </Group>
      </Table.Td>
      <Table.Td>
        <Text size="sm" c="dimmed">{classification.Prototypes?.length || 0} prototypes</Text>
      </Table.Td>
      <Table.Td>
        <Group gap={4} justify="flex-end">
          <ActionIcon
            variant="subtle"
            color="gray"
            onClick={() => openEditModal(classification)}
            aria-label="Edit classification"
          >
            <IconEdit style={{ width: rem(16), height: rem(16) }} />
          </ActionIcon>
          <ActionIcon
            variant="subtle"
            color="gray"
            onClick={() => openDeleteModal(classification)}
            aria-label="Delete classification"
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
            <h1 className="text-lg font-bold text-neutral-200 mb-2">CLASSIFICATION & PROTOTYPE MANAGEMENT</h1>
            <p className="text-neutral-400">Create and manage classifications with their associated prototypes</p>
          </div>
          <button 
            onClick={() => setCreateModalOpen(true)}
            className="btn-secondary text-sm flex items-center gap-2"
          >
            <IconPlus size={16} />
            Create New Classification
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
          {classifications.length === 0 ? (
            <div className="card p-8 text-center">
              <div className="w-16 h-16 bg-neutral-800 rounded-full flex items-center justify-center mx-auto mb-4">
                <IconEdit className="w-8 h-8 text-neutral-400" />
              </div>
              <h3 className="text-white text-lg font-semibold mb-2">No Classifications Found</h3>
              <p className="text-neutral-400">
                Create your first classification to get started organizing your prototypes.
              </p>
            </div>
          ) : (
            <Table highlightOnHover className="bg-neutral-900 border border-neutral-800">
              <Table.Thead>
                <Table.Tr>
                  <Table.Th className="text-neutral-200 font-semibold">Classification</Table.Th>
                  <Table.Th className="text-neutral-200 font-semibold">Prototypes</Table.Th>
                  <Table.Th style={{ width: 120 }} className="text-neutral-200 font-semibold">Actions</Table.Th>
                </Table.Tr>
              </Table.Thead>
              <Table.Tbody>{rows}</Table.Tbody>
            </Table>
          )}
        </div>
      )}

      {/* Create Classification Modal */}
      <Modal
        lockScroll={false}
        opened={createModalOpen}
        onClose={() => {
          setCreateModalOpen(false);
          setNewClassificationName('');
          setNameError('');
        }}
        title="Create New Classification"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200',
          close: 'text-neutral-400 hover:text-white'
        }}
      >
        <TextInput
          label="Classification Name"
          placeholder="Enter classification name"
          value={newClassificationName}
          onChange={(event) => {
            setNewClassificationName(event.currentTarget.value);
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
          <Text className="text-neutral-300 mb-2">Classification Color</Text>
          <ColorPicker
            value={newClassificationColor}
            onChange={setNewClassificationColor}
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
            onClick={handleCreateClassification}
            disabled={isSubmitting}
            className="btn-secondary text-sm flex items-center gap-2"
          >
            <IconCheck size={16} />
            Create Classification
          </button>
        </Group>
      </Modal>

      {/* Classification Prototype Modal */}
      <ClassificationPrototypeModal
        opened={editModalOpen}
        onClose={() => {
          setEditModalOpen(false);
          setClassificationToEdit(null);
        }}
        classification={classificationToEdit}
        onError={setError}
      />

      {/* Delete Classification Modal */}
      <Modal
        opened={deleteModalOpen}
        onClose={() => {
          setDeleteModalOpen(false);
          setClassificationToDelete(null);
        }}
        title="Delete Classification"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200',
          close: 'text-neutral-400 hover:text-white'
        }}
        lockScroll={false}
      >
        {classificationToDelete && (
          <div>
            <p className="text-neutral-200 mb-4 text-sm">
              Are you sure you want to delete the classification "<strong>{classificationToDelete.Name}</strong>"?
            </p>
            <p className="text-neutral-400 mb-6 text-sm">
              This will also delete all associated prototypes.
            </p>
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
                onClick={handleDeleteClassification}
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