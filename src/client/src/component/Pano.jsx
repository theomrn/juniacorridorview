import { Buffer } from 'buffer';
import React, {
  useEffect,
  useRef,
  useState,
  useCallback,
  useMemo,
} from "react";
import * as api from '../api/AxiosPano';
import { getTourSteps } from '../api/AxiosTour';
import { useTranslation } from 'react-i18next';
import '../style/Pano.css';
import { toast } from "sonner";
import Panorama360 from './Panorama360';
import Loader from "./Loader";
import Navbar from './Navbar';

const PanoramaViewer = ({ location, setSelectedImageName, setCurrentRoomNumber }) => {

  Buffer.from = Buffer.from || require('buffer').Buffer;

  const { t } = useTranslation('pano');

  // ------------------- STATES -------------------
  const [images, setImages] = useState([]);
  const [currentImageId, setCurrentImageId] = useState(null);
  const [infoPopups, setInfoPopups] = useState({});
  const [links, setLinks] = useState({});
  const [currentRoomName, setCurrentRoomName] = useState('');
  const [currentRoomNumberState, setCurrentRoomNumberState] = useState('');
  const [rooms, setRooms] = useState([]);
  const [roomPreviews, setRoomPreviews] = useState({});
  const [previewFlags, setPreviewFlags] = useState({});
  const [visitType, setVisitType] = useState(t('freeTour'));
  const [tourSteps, setTourSteps] = useState([]);
  const [allRoomImages, setAllRoomImages] = useState({});
  const [currentFloor, setCurrentFloor] = useState(null);
  const [loading, setLoading] = useState(true);
  const [textLoading, setTextLoading] = useState(t('loading'));

  // ------------------- REFS -------------------
  const firstLoad = useRef(true);
  const isLoading = useRef(true);
  const dataFetched = useRef(false);
  const loadingImage = useRef(false);
  const [loadingImageBeforeRoomSwitch, setLoadingImageBeforeRoomSwitch] = useState(false);

  // ------------------- HELPERS -------------------
  const showLoading = useCallback((promises, textLoading, textSuccess, textError) => {
    setLoading(true);
    setTextLoading(textLoading);

    Promise.all(promises)
      .then(() => {
        setLoading(false);
        toast.success(textSuccess);
      })
      .catch((error) => {
        setLoading(false);
        console.error('Error fetching data:', error);
        toast.error(textError);
      });
  }, []);

  const cleanUrlParams = () => {
    const url = new URL(window.location);
    url.search = '';
    window.history.replaceState({}, document.title, url);
  };

  // ------------------- API HELPERS -------------------
  const fetchRoomDetails = async (id_rooms) => {
    const room = await api.getRoomDetails(id_rooms);
    setCurrentRoomName(room.name);
    setCurrentRoomNumberState(room.number);
    setSelectedImageName?.(room.name);
    setCurrentRoomNumber?.(room.number);
  };

  const fetchFloor = async (id_rooms) => {
    const room = rooms.find(r => r.id_rooms === id_rooms);
    if (!room) return;

    const floor = await api.getFloorById(room.id_floors);
    setCurrentFloor({ ...floor, plan_x: room.plan_x, plan_y: room.plan_y });
  };

  const retrieveImageData = async (pictureIds) => {
    return Promise.all(
      pictureIds.map(async picture => {
        const imagePath = await api.getImage(picture.id_pictures);
        return {
          id: picture.id_pictures,
          imageUrl: `http://localhost:5078/${imagePath}`,
        };
      })
    );
  };

  // ------------------- FETCH ALL DATA -------------------
  const fetchAllData = useCallback(async () => {
    try {
      isLoading.current = true;

      const params = new URLSearchParams(location.search);
      const tourId = params.get('tour_id');

      let stepsData = [];
      let roomsData = [];

      if (tourId) {
        stepsData = await getTourSteps(tourId);
        setTourSteps(stepsData);

        roomsData = await Promise.all(
          stepsData.map(async step => {
            const room = await api.getRoomDetails(step.id_rooms);
            return { ...room, id_rooms: step.id_rooms };
          })
        );

        roomsData = roomsData.filter(r => r.hidden !== 1);
        setVisitType(`${t('guidedTour')}, Parcours ${tourId}`);
      } else {
        roomsData = await api.getRooms();
        roomsData = roomsData.filter(r => r.hidden !== 1);
      }

      setRooms(roomsData);

      // ---- Fetch previews or first panorama ----
      const previewPromises = roomsData.map(async room => {
        try {
          const previewUrl = await api.getRoomPreview(room.id_rooms);
          if (previewUrl) {
            return { id_rooms: room.id_rooms, imageUrl: previewUrl, isPreview: true };
          }

          const pictures = await api.getPicturesByRoomId(room.id_rooms);
          if (!pictures.length) return { id_rooms: room.id_rooms, imageUrl: null, isPreview: false };

          const img = await api.getImage(pictures[0].id_pictures);
          return { id_rooms: room.id_rooms, imageUrl: img, isPreview: false };

        } catch (e) {
          console.error(`Preview error for room ${room.id_rooms}:`, e);
          return { id_rooms: room.id_rooms, imageUrl: null, isPreview: false };
        }
      });

      const roomPreviewsData = await Promise.all(previewPromises);

      // Set previews
      const previewMap = {};
      const previewFlagsMap = {};

      for (const prev of roomPreviewsData) {
        previewFlagsMap[prev.id_rooms] = prev.isPreview;
        if (prev.isPreview) previewMap[prev.id_rooms] = prev.imageUrl;
      }

      setRoomPreviews(previewMap);
      setPreviewFlags(previewFlagsMap);

      // ---- Fetch all panoramas ----
      const roomImagesPromises = roomsData.map(async room => {
        const pictures = await api.getPicturesByRoomId(room.id_rooms);
        const images = await retrieveImageData(pictures);
        return { id_rooms: room.id_rooms, images };
      });

      const allRoomImagesData = await Promise.all(roomImagesPromises);

      const allImagesMap = {};
      const mainImages = [];

      allRoomImagesData.forEach(room => {
        allImagesMap[room.id_rooms] = room.images;
        mainImages.push(...room.images);
      });

      setAllRoomImages(allImagesMap);
      setImages(mainImages);

      isLoading.current = false;

    } catch (error) {
      console.error("Erreur lors du chargement des données :", error);
    }
  }, [location.search, rooms]);

  // ------------------- DISPLAY IMAGE -------------------
  const displayImage = async (id) => {
    cleanUrlParams();
    if (currentImageId !== id) setCurrentImageId(id);

    const popupsPromise = api.getInfoPopup(id);
    const linksPromise = api.getLinks(id);

    const roomIdPromise = api.getRoomIdByPictureId(id).then((roomId) => {
      fetchRoomDetails(roomId);
      fetchFloor(roomId);
    });

    if (!isLoading.current || firstLoad.current) {
      showLoading(
        [popupsPromise, linksPromise, roomIdPromise],
        t('currentLoading'),
        t('loadingSuccess'),
        t('loadingError')
      );
    }

    popupsPromise.then(p => setInfoPopups(prev => ({ ...prev, [id]: p })));
    linksPromise.then(l => setLinks(prev => ({ ...prev, [id]: l })));

    Promise.all([popupsPromise, linksPromise, roomIdPromise]).then(() => {
      setLoadingImageBeforeRoomSwitch(false);
    });
  };

  // ------------------- EVENTS -------------------
  const handleRoomClick = (id_rooms) => {
    setLoadingImageBeforeRoomSwitch(true);

    const pictures = allRoomImages[id_rooms];
    if (pictures?.length > 0) {
      displayImage(pictures[0].id);
    }
  };

  const handleLinkClick = (id_pictures_destination) => {
    setLoadingImageBeforeRoomSwitch(true);
    const image = images.find(img => img.id === id_pictures_destination);
    if (image) displayImage(image.id);
  };

  // ------------------- EFFECTS -------------------
  useEffect(() => {
    if (!dataFetched.current) {
      fetchAllData();
      dataFetched.current = true;
    }
  }, [fetchAllData]);

  useEffect(() => {
    if (images.length > 0 && !isLoading.current && firstLoad.current) {
      if (!loadingImage.current) {
        loadingImage.current = true;
        displayImage(images[0].id);
      }
      firstLoad.current = false;
    }
  }, [images]);

  // ------------------- MEMO -------------------
  const filteredRooms = useMemo(() => {
    const visible = rooms.filter(r => r.hidden !== 1);
    visible.sort((a, b) => a.number.localeCompare(b.number));

    return visitType.startsWith('Visite guidée')
      ? visible.filter(room => tourSteps.some(step => step.id_rooms === room.id_rooms))
      : visible;

  }, [rooms, tourSteps, visitType]);

  // ------------------- RENDER -------------------
  return (
    <div>
      <Loader show={loading} text={textLoading} />

      <div className="panorama-container bg-junia-lavender">

        {/* SIDEBAR – ROOMS LIST */}
        <div className="h-full scrollable-list flex-col w-15" id="style-2">
          <div className="other-rooms-title">{t('otherRooms')}</div>

          {filteredRooms.map(room => (
            <div
              key={room.id_rooms}
              className={`room-container ${currentRoomName === room.name ? 'selected-room' : ''}`}
              onClick={() => handleRoomClick(room.id_rooms)}
            >
              {roomPreviews[room.id_rooms] && (
                <img src={roomPreviews[room.id_rooms]} alt={`Preview of ${room.name}`} />
              )}

              <div className="bg-white text-center font-bold text-junia-orange room-title border-junia-orange">
                {room.number} - {room.name}
              </div>
            </div>
          ))}
        </div>

        {/* PANORAMA */}
        <div className="panorama-content">
          <Panorama360
            infoPopups={infoPopups[currentImageId] || []}
            selectedPicture={images.find(img => img.id === currentImageId) || null}
            links={links[currentImageId] || []}
            onLinkClick={handleLinkClick}
            isLoading={isLoading.current || firstLoad.current || loadingImageBeforeRoomSwitch}
            floor={currentFloor}
          />
        </div>

      </div>
    </div>
  );
};

export default PanoramaViewer;
