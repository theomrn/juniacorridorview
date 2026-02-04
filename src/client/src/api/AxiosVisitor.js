import axios from "axios";

export const getVisitorTypes = async () => {
    try {
        const response = await axios.get('/api/visitor-types');
        return response.data;
    } catch (error) {
        console.error('Error fetching visitor types:', error);
        return [];
    }
};

export const createVisitorType = async (name) => {
    try {
        const response = await axios.post('/api/insert-visitor-type', { "name":name });
        return response.data;
    } catch (error) {
        console.error('Error creating visitor type:', error);
        throw error;
    }
};

export const updateVisitorType = async (id_visitor_type, name) => {
    try {
        const response = await axios.put(`/api/update-visitor-type/${id_visitor_type}`, { name });
        return response.data;
    } catch (error) {
        console.error('Error updating visitor type:', error);
        throw error;
    }
};

export const deleteVisitorType = async (id_visitor_type) => {
    try {
        const response = await axios.delete(`/api/visitors/types/${id_visitor_type}`);
        return response.data;
    } catch (error) {
        console.error('Error deleting visitor type:', error);
        throw error;
    }
};
