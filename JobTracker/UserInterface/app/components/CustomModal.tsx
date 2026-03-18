import React from 'react';
import { Modal, ModalProps } from '@mantine/core';
import { useScrollLock } from '../hooks/useScrollLock';

interface CustomModalProps extends Omit<ModalProps, 'lockScroll'> {
  lockScroll?: boolean;
}

export function CustomModal({ 
  opened, 
  lockScroll = true, 
  onClose, 
  ...props 
}: CustomModalProps) {
  useScrollLock(opened && lockScroll);

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      {...props}
    />
  );
}
