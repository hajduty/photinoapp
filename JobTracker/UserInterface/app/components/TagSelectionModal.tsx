'use client';

import React from 'react';
import { Modal } from '@mantine/core';
import { IconX, IconCheck } from '@tabler/icons-react';

interface Tag {
  Id: number;
  Name: string;
  Color: string;
}

interface TagSelectionModalProps {
  opened: boolean;
  onClose: () => void;
  onConfirm: (tagIds: number[]) => void;
  tags: Tag[];
  selectedTagIds: number[];
  onToggleTag: (tagId: number) => void;
}

export function TagSelectionModal({
  opened,
  onClose,
  onConfirm,
  tags,
  selectedTagIds,
  onToggleTag,
}: TagSelectionModalProps) {
  const handleConfirm = () => {
    if (selectedTagIds.length > 0) {
      onConfirm(selectedTagIds);
    }
  };

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      title="Select tags to deprioritize"
      centered
      size="sm"
    >
      <div className="space-y-4">
        <p className="text-sm text-neutral-400">
          Which tags made this job a bad match? Selected tags will be deprioritized in future recommendations.
        </p>

        <div className="flex flex-wrap gap-2 max-h-[300px] overflow-y-auto p-1">
          {tags.map((tag) => {
            const isSelected = selectedTagIds.includes(tag.Id);
            return (
              <button
                key={tag.Id}
                onClick={() => onToggleTag(tag.Id)}
                className={`inline-flex items-center gap-1.5 px-3 py-1.5 rounded text-xs font-semibold uppercase tracking-wide transition-all ${
                  isSelected
                    ? 'ring-2 ring-white ring-offset-1 ring-offset-neutral-900'
                    : 'opacity-60 hover:opacity-100'
                }`}
                style={{
                  backgroundColor: tag.Color,
                  color: getContrastColor(tag.Color),
                }}
              >
                {isSelected && <IconCheck size={12} />}
                {tag.Name}
              </button>
            );
          })}
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <button
            onClick={onClose}
            className="px-3 py-1.5 text-xs rounded border border-neutral-700 text-neutral-400 hover:bg-neutral-800 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={handleConfirm}
            disabled={selectedTagIds.length === 0}
            className="px-3 py-1.5 text-xs rounded bg-red-600 text-white hover:bg-red-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Confirm {selectedTagIds.length > 0 && `(${selectedTagIds.length})`}
          </button>
        </div>
      </div>
    </Modal>
  );
}

function getContrastColor(hexColor: string): string {
  const hex = hexColor.replace('#', '');
  const r = parseInt(hex.substring(0, 2), 16);
  const g = parseInt(hex.substring(2, 4), 16);
  const b = parseInt(hex.substring(4, 6), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.5 ? '#000000' : '#ffffff';
}
