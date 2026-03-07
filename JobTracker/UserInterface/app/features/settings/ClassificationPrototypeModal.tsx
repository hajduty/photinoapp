import React from 'react';
import {
  Modal,
  Group,
  Loader,
} from '@mantine/core';
import {
  IconPlus as IconAdd,
  IconTrash as IconDelete,
  IconX
} from '@tabler/icons-react';
import { Classification } from '../../types/classifications/classification';
import { Prototype } from '../../types/prototypes/prototype';
import { usePrototypesByClassification, useCreatePrototype, useDeletePrototype } from '../../hooks/usePrototypes';

interface ClassificationPrototypeModalProps {
  opened: boolean;
  onClose: () => void;
  classification: Classification | null;
  onError: (error: string) => void;
}

export default function ClassificationPrototypeModal({
  opened,
  onClose,
  classification,
  onError
}: ClassificationPrototypeModalProps) {
  const [newPrototypeText, setNewPrototypeText] = React.useState('');
  const [prototypeToDelete, setPrototypeToDelete] = React.useState<Prototype | null>(null);
  const [deletePrototypeModalOpen, setDeletePrototypeModalOpen] = React.useState(false);
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  // Use TanStack Query hooks
  const { data: classificationPrototypes = [], isLoading: prototypeLoading } = usePrototypesByClassification(classification?.Id || 0);
  const createPrototypeMutation = useCreatePrototype();
  const deletePrototypeMutation = useDeletePrototype();

  const handleAddPrototype = async () => {
    if (!classification || !newPrototypeText.trim()) return;
    try {
      setIsSubmitting(true);
      await createPrototypeMutation.mutateAsync({
        ClassificationId: classification.Id,
        Text: newPrototypeText.trim()
      });
      setNewPrototypeText('');
    } catch (err) {
      console.error('Failed to create prototype:', err);
      onError('Failed to create prototype. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDeletePrototype = async () => {
    if (!prototypeToDelete) return;
    try {
      setIsSubmitting(true);
      await deletePrototypeMutation.mutateAsync({ PrototypeId: prototypeToDelete.Id });
      setDeletePrototypeModalOpen(false);
      setPrototypeToDelete(null);
    } catch (err) {
      console.error('Failed to delete prototype:', err);
      onError('Failed to delete prototype. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const openDeletePrototypeModal = (prototype: Prototype) => {
    setPrototypeToDelete(prototype);
    setDeletePrototypeModalOpen(true);
  };

  const closeDeletePrototypeModal = () => {
    setDeletePrototypeModalOpen(false);
    setPrototypeToDelete(null);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') handleAddPrototype();
  };

  return (
    <>
      <Modal
        lockScroll={false}
        opened={opened}
        onClose={onClose}
        title={
          <div className="flex items-center gap-2">
            <span className="text-sm font-semibold text-neutral-200">
              {classification?.Name}
            </span>
          </div>
        }
        size="lg"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200',
          close: 'text-neutral-400 hover:text-white'
        }}
      >
        <div className="space-y-4">
          {/* Add Prototype */}
          <div className="flex gap-2">
            <input
              type="text"
              placeholder="Enter prototype text..."
              value={newPrototypeText}
              onChange={(e) => setNewPrototypeText(e.target.value)}
              onKeyDown={handleKeyDown}
              className="input-field flex-1 text-sm"
            />
            <button
              onClick={handleAddPrototype}
              disabled={isSubmitting || !newPrototypeText.trim()}
              className="btn-secondary text-xs flex items-center gap-1.5 px-3 flex-shrink-0"
            >
              <IconAdd size={14} />
              Add
            </button>
          </div>

          {/* Prototypes List */}
          <div>
            {prototypeLoading ? (
              <div className="flex items-center justify-center py-6">
                <Loader size="xs" />
              </div>
            ) : classificationPrototypes.length === 0 ? (
              <div className="text-center py-6 text-neutral-500 text-xs">
                No prototypes yet — add one above.
              </div>
            ) : (
              <div className="space-y-1 max-h-60 overflow-y-auto custom-scrollbar pr-1">
                {classificationPrototypes.map((prototype) => (
                  <div
                    key={prototype.Id}
                    className="flex items-center justify-between bg-neutral-800 hover:bg-neutral-800/80 rounded px-3 py-2 group transition-colors"
                  >
                    <p className="text-neutral-300 text-xs flex-1 leading-relaxed">
                      {prototype.Text}
                    </p>
                    <button
                      onClick={() => openDeletePrototypeModal(prototype)}
                      aria-label="Delete prototype"
                      className="btn-ghost p-1 ml-2 opacity-0 group-hover:opacity-100 transition-opacity text-neutral-500 hover:text-red-400"
                    >
                      <IconDelete size={13} />
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        <Group justify="flex-end" mt="md" pt="xs">
          <button
            onClick={onClose}
            disabled={isSubmitting}
            className="btn-ghost text-xs"
          >
            Close
          </button>
        </Group>
      </Modal>

      {/* Delete Prototype Confirmation Modal */}
      <Modal
        opened={deletePrototypeModalOpen}
        onClose={closeDeletePrototypeModal}
        title="Delete Prototype"
        size="sm"
        centered
        classNames={{
          content: 'bg-neutral-900 border border-neutral-800',
          title: 'text-neutral-200',
          close: 'text-neutral-400 hover:text-white'
        }}
        lockScroll={false}
      >
        {prototypeToDelete && (
          <div className="space-y-3">
            <p className="text-neutral-400 text-xs">
              Are you sure you want to delete this prototype?
            </p>
            <div className="bg-neutral-800 rounded px-3 py-2">
              <p className="text-neutral-300 text-xs leading-relaxed">
                {prototypeToDelete.Text}
              </p>
            </div>
            <div className="flex justify-end gap-2 pt-1">
              <button
                onClick={closeDeletePrototypeModal}
                disabled={isSubmitting}
                className="btn-ghost text-xs flex items-center gap-1.5"
              >
                <IconX size={13} />
                Cancel
              </button>
              <button
                onClick={handleDeletePrototype}
                disabled={isSubmitting}
                className="btn-secondary text-xs flex items-center gap-1.5"
              >
                <IconDelete size={13} />
                Delete
              </button>
            </div>
          </div>
        )}
      </Modal>
    </>
  );
}