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
  transitionProps = { transition: 'fade', duration: 200, timingFunction: 'ease' },
  ...props 
}: CustomModalProps) {
  useScrollLock(opened && lockScroll);

  return (
    <Modal
      opened={opened}
      onClose={onClose}
      transitionProps={transitionProps}
      {...props}
    />
  );
}
