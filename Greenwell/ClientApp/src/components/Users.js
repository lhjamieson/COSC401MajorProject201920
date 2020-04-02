import React from 'react';
import { List, Datagrid, TextField, EmailField } from 'react-admin';
export const UserList = props => (
    <List {...props}>
        <Datagrid rowClick="edit">
            <TextField source="name" />
            <EmailField source="email" />
            <TextField source="role" />
        </Datagrid>
    </List>
);