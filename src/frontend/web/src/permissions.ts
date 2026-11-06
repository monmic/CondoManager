import { createContext, useContext } from 'react'

export const WritePermission = createContext(false)
export const useCanWrite = () => useContext(WritePermission)